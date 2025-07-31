using Microsoft.EntityFrameworkCore;
using Api.Data;
using Api.Models;
using Api.DTOs;
using Api.Extensions;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add Entity Framework
builder.Services.AddDbContext<CalcioDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
        o => o.UseNetTopologySuite()));

// Add API Explorer and Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Calcio API",
        Version = "v1",
        Description = "API for managing football clubs, teams, matches, and venues"
    });
});

// Configure JSON serialization to handle circular references
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    options.SerializerOptions.WriteIndented = true;
});

// Add CORS for development
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Automatic database migration on startup
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<CalcioDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Checking for pending database migrations...");
        var pendingMigrations = await context.Database.GetPendingMigrationsAsync();

        if (pendingMigrations.Any())
        {
            logger.LogInformation($"Applying {pendingMigrations.Count()} pending migrations...");
            await context.Database.MigrateAsync();
            logger.LogInformation("Database migrations completed successfully.");
        }
        else
        {
            logger.LogInformation("Database is up to date.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while migrating the database.");
        throw; // Re-throw to prevent the application from starting with an invalid database state
    }
}

// Configure Swagger (enable in all environments for this demo)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Calcio API v1");
    c.RoutePrefix = "swagger"; // Access via /swagger
});

// Use CORS
app.UseCors("AllowAll");

var api = app.MapGroup("/api").WithTags("API");

// Root endpoint
app.MapGet("/", () => "Calcio API is running!")
.WithName("Root")
.WithSummary("API status")
.WithDescription("Returns a simple status message")
.WithTags("Status");

var clubs = api.MapGroup("/clubs").WithTags("Clubs");

clubs.MapGet("/", async (CalcioDbContext db) =>
{
    var clubs = await db.Clubs.Include(c => c.Teams).ToListAsync();
    return clubs.Select(c => c.ToDto()).ToList();
})
.WithName("GetClubs")
.WithSummary("Get all clubs")
.WithDescription("Retrieves all football clubs with their associated teams")
.Produces<List<ClubDto>>();

clubs.MapPost("/find-or-create", async (FindOrCreateClubRequest request, CalcioDbContext db) =>
{
    var existingClub = await db.Clubs.FirstOrDefaultAsync(c => c.ExternalId == request.ExternalId);
    if (existingClub != null)
    {
        existingClub.Name = request.Name;
        await db.SaveChangesAsync();
        return Results.Ok(existingClub.ToDto());
    }

    var newClub = new Club { ExternalId = request.ExternalId, Name = request.Name };
    db.Clubs.Add(newClub);
    await db.SaveChangesAsync();
    return Results.Created($"/api/clubs/{newClub.Id}", newClub.ToDto());
})
.WithName("FindOrCreateClub")
.WithSummary("Find or create club by external ID")
.WithDescription("Finds existing club by external ID or creates a new one")
.Accepts<FindOrCreateClubRequest>("application/json")
.Produces<ClubDto>(201)
.Produces<ClubDto>(200);

var teams = api.MapGroup("/teams").WithTags("Teams");

teams.MapPost("/find-or-create", async (FindOrCreateTeamRequest request, CalcioDbContext db) =>
{
    // First try to find by external ID if provided
    if (!string.IsNullOrEmpty(request.ExternalId))
    {
        var existingTeam = await db.Teams.Include(t => t.Club)
            .FirstOrDefaultAsync(t => t.ExternalId == request.ExternalId);
        if (existingTeam != null)
        {
            existingTeam.Name = request.Name;
            await db.SaveChangesAsync();
            return Results.Ok(existingTeam.ToDto());
        }
    }

    // Find the club first
    var club = await db.Clubs.FirstOrDefaultAsync(c => c.ExternalId == request.ClubExternalId);
    if (club == null)
    {
        return Results.BadRequest($"Club with external ID {request.ClubExternalId} not found");
    }

    // Try to find existing team by name and club
    var existingTeamByName = await db.Teams.Include(t => t.Club)
        .FirstOrDefaultAsync(t => t.Name == request.Name && t.ClubId == club.Id);
    if (existingTeamByName != null)
    {
        existingTeamByName.ExternalId = request.ExternalId; // Update external ID if provided
        await db.SaveChangesAsync();
        return Results.Ok(existingTeamByName.ToDto());
    }

    // Create new team
    var newTeam = new Team
    {
        Name = request.Name,
        ClubId = club.Id,
        ExternalId = request.ExternalId
    };
    db.Teams.Add(newTeam);
    await db.SaveChangesAsync();

    // Load the team with club for DTO
    var createdTeam = await db.Teams.Include(t => t.Club)
        .FirstAsync(t => t.Id == newTeam.Id);

    return Results.Created($"/api/teams/{newTeam.Id}", createdTeam.ToDto());
})
.WithName("FindOrCreateTeam")
.WithSummary("Find or create team")
.WithDescription("Finds existing team by external ID or name+club, or creates a new one")
.Accepts<FindOrCreateTeamRequest>("application/json")
.Produces<TeamDto>(201)
.Produces<TeamDto>(200)
.Produces(400);

var venues = api.MapGroup("/venues").WithTags("Venues");

venues.MapPost("/find-or-create", async (CreateVenueRequest request, CalcioDbContext db) =>
{
    // Check if venue already exists by address
    var existingVenue = await db.Venues.FirstOrDefaultAsync(v => v.Address == request.Address);
    if (existingVenue != null)
    {
        // Update coordinates if provided
        if (request.Latitude.HasValue && request.Longitude.HasValue)
        {
            existingVenue.Location = new NetTopologySuite.Geometries.Point(
                request.Longitude.Value, request.Latitude.Value)
            {
                SRID = 4326
            };
            await db.SaveChangesAsync();
        }
        return Results.Ok(existingVenue.ToDto());
    }

    var venue = new Venue { Address = request.Address };

    // Set location if coordinates provided
    if (request.Latitude.HasValue && request.Longitude.HasValue)
    {
        venue.Location = new NetTopologySuite.Geometries.Point(
            request.Longitude.Value, request.Latitude.Value)
        {
            SRID = 4326
        };
    }

    db.Venues.Add(venue);
    await db.SaveChangesAsync();
    return Results.Created($"/api/venues/{venue.Id}", venue.ToDto());
})
.WithName("FindOrCreateVenue")
.WithSummary("Find or create venue by address")
.WithDescription("Finds existing venue by address or creates a new one with optional coordinates")
.Accepts<CreateVenueRequest>("application/json")
.Produces<VenueDto>(201)
.Produces<VenueDto>(200);

var ageGroups = api.MapGroup("/age-groups").WithTags("Age Groups");

ageGroups.MapPost("/find-or-create", async (UpsertRequest request, CalcioDbContext db) =>
{
    var existing = await db.AgeGroups.FirstOrDefaultAsync(ag => ag.Name == request.Name);
    if (existing != null)
    {
        return Results.Ok(existing.ToDto());
    }

    var newAgeGroup = new AgeGroup { Name = request.Name };
    db.AgeGroups.Add(newAgeGroup);
    await db.SaveChangesAsync();
    return Results.Created($"/api/age-groups/{newAgeGroup.Id}", newAgeGroup.ToDto());
})
.WithName("FindOrCreateAgeGroup")
.WithSummary("Find or create age group by name")
.WithDescription("Finds existing age group by name or creates a new one")
.Accepts<UpsertRequest>("application/json")
.Produces<AgeGroupDto>(201)
.Produces<AgeGroupDto>(200);

var competitions = api.MapGroup("/competitions").WithTags("Competitions");

competitions.MapPost("/find-or-create", async (UpsertRequest request, CalcioDbContext db) =>
{
    var existing = await db.Competitions.FirstOrDefaultAsync(c => c.Name == request.Name);
    if (existing != null)
    {
        return Results.Ok(existing.ToDto());
    }

    var newCompetition = new Competition { Name = request.Name };
    db.Competitions.Add(newCompetition);
    await db.SaveChangesAsync();
    return Results.Created($"/api/competitions/{newCompetition.Id}", newCompetition.ToDto());
})
.WithName("FindOrCreateCompetition")
.WithSummary("Find or create competition by name")
.WithDescription("Finds existing competition by name or creates a new one")
.Accepts<UpsertRequest>("application/json")
.Produces<CompetitionDto>(201)
.Produces<CompetitionDto>(200);

var matches = api.MapGroup("/matches").WithTags("Matches");

matches.MapPost("/", async (Match match, CalcioDbContext db) =>
{
    db.Matches.Add(match);
    await db.SaveChangesAsync();
    return Results.Created($"/api/matches/{match.Id}", match);
})
.WithName("CreateMatch")
.WithSummary("Create a new match")
.WithDescription("Creates a new match")
.Accepts<Match>("application/json")
.Produces<Match>(201);

// Lookup endpoints for ID-only responses (for crawler compatibility)
var lookup = api.MapGroup("/lookup").WithTags("Lookup");

lookup.MapGet("/clubs/{externalId}/id", async (string externalId, CalcioDbContext db) =>
{
    var club = await db.Clubs.FirstOrDefaultAsync(c => c.ExternalId == externalId);
    return club is not null ? Results.Ok(club.Id) : Results.NotFound();
})
.WithName("GetClubIdByExternalId")
.WithSummary("Get club ID by external ID")
.WithDescription("Returns only the ID of a club by its external ID")
.Produces<int>()
.Produces(404);

lookup.MapGet("/age-groups/{name}/id", async (string name, CalcioDbContext db) =>
{
    var ageGroup = await db.AgeGroups.FirstOrDefaultAsync(ag => ag.Name == name);
    return ageGroup is not null ? Results.Ok(ageGroup.Id) : Results.NotFound();
})
.WithName("GetAgeGroupIdByName")
.WithSummary("Get age group ID by name")
.WithDescription("Returns only the ID of an age group by its name")
.Produces<int>()
.Produces(404);

lookup.MapGet("/competitions/{name}/id", async (string name, CalcioDbContext db) =>
{
    var competition = await db.Competitions.FirstOrDefaultAsync(c => c.Name == name);
    return competition is not null ? Results.Ok(competition.Id) : Results.NotFound();
})
.WithName("GetCompetitionIdByName")
.WithSummary("Get competition ID by name")
.WithDescription("Returns only the ID of a competition by its name")
.Produces<int>()
.Produces(404);

lookup.MapGet("/venues/{address}/id", async (string address, CalcioDbContext db) =>
{
    var venue = await db.Venues.FirstOrDefaultAsync(v => v.Address == address);
    return venue is not null ? Results.Ok(venue.Id) : Results.NotFound();
})
.WithName("GetVenueIdByAddress")
.WithSummary("Get venue ID by address")
.WithDescription("Returns only the ID of a venue by its address")
.Produces<int>()
.Produces(404);

app.Run();
