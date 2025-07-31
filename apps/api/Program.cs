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

// Clubs endpoints
app.MapGet("/api/clubs", async (CalcioDbContext db) =>
{
    var clubs = await db.Clubs.Include(c => c.Teams).ToListAsync();
    return clubs.Select(c => c.ToDto()).ToList();
})
.WithName("GetClubs")
.WithSummary("Get all clubs")
.WithDescription("Retrieves all football clubs with their associated teams")
.WithTags("Clubs")
.Produces<List<ClubDto>>();

app.MapGet("/api/clubs/{id}", async (int id, CalcioDbContext db) =>
{
    var club = await db.Clubs.Include(c => c.Teams).FirstOrDefaultAsync(c => c.Id == id);
    return club is not null ? Results.Ok(club.ToDto()) : Results.NotFound();
})
.WithName("GetClubById")
.WithSummary("Get club by ID")
.WithDescription("Retrieves a specific club by its ID")
.WithTags("Clubs")
.Produces<ClubDto>()
.Produces(404);

app.MapGet("/api/clubs/external/{externalId}", async (string externalId, CalcioDbContext db) =>
{
    var club = await db.Clubs.Include(c => c.Teams).FirstOrDefaultAsync(c => c.ExternalId == externalId);
    return club is not null ? Results.Ok(club.ToDto()) : Results.NotFound();
})
.WithName("GetClubByExternalId")
.WithSummary("Get club by external ID")
.WithDescription("Retrieves a specific club by its external ID")
.WithTags("Clubs")
.Produces<ClubDto>()
.Produces(404);

app.MapPost("/api/clubs", async (Club club, CalcioDbContext db) =>
{
    db.Clubs.Add(club);
    await db.SaveChangesAsync();
    return Results.Created($"/api/clubs/{club.Id}", club);
})
.WithName("CreateClub")
.WithSummary("Create a new club")
.WithDescription("Creates a new football club")
.WithTags("Clubs")
.Accepts<Club>("application/json")
.Produces<Club>(201);

// Find or create club endpoint for crawler
app.MapPost("/api/clubs/find-or-create", async (FindOrCreateClubRequest request, CalcioDbContext db) =>
{
    var existingClub = await db.Clubs.FirstOrDefaultAsync(c => c.ExternalId == request.ExternalId);
    if (existingClub != null)
    {
        return Results.Ok(existingClub.ToDto());
    }

    var newClub = new Club { ExternalId = request.ExternalId, Name = request.Name };
    db.Clubs.Add(newClub);
    await db.SaveChangesAsync();
    return Results.Created($"/api/clubs/{newClub.Id}", newClub.ToDto());
})
.WithName("FindOrCreateClub")
.WithSummary("Find existing club or create new one")
.WithDescription("Finds a club by external ID, or creates a new one if not found")
.WithTags("Clubs")
.Accepts<FindOrCreateClubRequest>("application/json")
.Produces<ClubDto>(201)
.Produces<ClubDto>(200);

// Teams endpoints
app.MapGet("/api/teams", async (CalcioDbContext db) =>
{
    var teams = await db.Teams.Include(t => t.Club).ToListAsync();
    return teams.Select(t => t.ToDto()).ToList();
});

app.MapGet("/api/teams/{id}", async (int id, CalcioDbContext db) =>
{
    var team = await db.Teams.Include(t => t.Club).FirstOrDefaultAsync(t => t.Id == id);
    return team is not null ? Results.Ok(team.ToDto()) : Results.NotFound();
});

app.MapPost("/api/teams", async (Team team, CalcioDbContext db) =>
{
    db.Teams.Add(team);
    await db.SaveChangesAsync();
    return Results.Created($"/api/teams/{team.Id}", team);
});

// Find or create team endpoint for crawler
app.MapPost("/api/teams/find-or-create", async (FindOrCreateTeamRequest request, CalcioDbContext db) =>
{
    // First try to find by external ID if provided
    if (!string.IsNullOrEmpty(request.ExternalId))
    {
        var existingTeam = await db.Teams.Include(t => t.Club)
            .FirstOrDefaultAsync(t => t.ExternalId == request.ExternalId);
        if (existingTeam != null)
        {
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
.WithSummary("Find existing team or create new one")
.WithDescription("Finds a team by external ID or name+club, or creates a new one if not found")
.WithTags("Teams")
.Accepts<FindOrCreateTeamRequest>("application/json")
.Produces<TeamDto>(201)
.Produces<TeamDto>(200)
.Produces(400);

// Venues endpoints
app.MapGet("/api/venues", async (CalcioDbContext db) =>
{
    var venues = await db.Venues.ToListAsync();
    return venues.Select(v => v.ToDto()).ToList();
});

app.MapGet("/api/venues/{id}", async (int id, CalcioDbContext db) =>
{
    var venue = await db.Venues.FirstOrDefaultAsync(v => v.Id == id);
    return venue is not null ? Results.Ok(venue.ToDto()) : Results.NotFound();
});

app.MapPost("/api/venues", async (Venue venue, CalcioDbContext db) =>
{
    db.Venues.Add(venue);
    await db.SaveChangesAsync();
    return Results.Created($"/api/venues/{venue.Id}", venue);
});

// Create venue with coordinates for crawler
app.MapPost("/api/venues/create", async (CreateVenueRequest request, CalcioDbContext db) =>
{
    // Check if venue already exists by address
    var existingVenue = await db.Venues.FirstOrDefaultAsync(v => v.Address == request.Address);
    if (existingVenue != null)
    {
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
.WithName("CreateVenueWithCoordinates")
.WithSummary("Create venue with optional coordinates")
.WithDescription("Creates a venue with address and optional latitude/longitude coordinates")
.WithTags("Venues")
.Accepts<CreateVenueRequest>("application/json")
.Produces<VenueDto>(201)
.Produces<VenueDto>(200);

// Get venue by address
app.MapGet("/api/venues/by-address/{address}", async (string address, CalcioDbContext db) =>
{
    var venue = await db.Venues.FirstOrDefaultAsync(v => v.Address == address);
    return venue is not null ? Results.Ok(venue.ToDto()) : Results.NotFound();
})
.WithName("GetVenueByAddress")
.WithSummary("Get venue by address")
.WithDescription("Retrieves a venue by its address")
.WithTags("Venues")
.Produces<VenueDto>()
.Produces(404);

// Age Groups endpoints
app.MapGet("/api/age-groups", async (CalcioDbContext db) =>
{
    var ageGroups = await db.AgeGroups.ToListAsync();
    return ageGroups.Select(ag => ag.ToDto()).ToList();
});

app.MapGet("/api/age-groups/{id}", async (int id, CalcioDbContext db) =>
{
    var ageGroup = await db.AgeGroups.FirstOrDefaultAsync(ag => ag.Id == id);
    return ageGroup is not null ? Results.Ok(ageGroup.ToDto()) : Results.NotFound();
});

app.MapPost("/api/age-groups", async (AgeGroup ageGroup, CalcioDbContext db) =>
{
    db.AgeGroups.Add(ageGroup);
    await db.SaveChangesAsync();
    return Results.Created($"/api/age-groups/{ageGroup.Id}", ageGroup);
});

// Upsert age group (find or create)
app.MapPost("/api/age-groups/upsert", async (UpsertRequest request, CalcioDbContext db) =>
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
.WithName("UpsertAgeGroup")
.WithSummary("Find or create age group")
.WithDescription("Finds an existing age group by name, or creates a new one if not found")
.WithTags("Age Groups")
.Accepts<UpsertRequest>("application/json")
.Produces<AgeGroupDto>(201)
.Produces<AgeGroupDto>(200);

// Competitions endpoints
app.MapGet("/api/competitions", async (CalcioDbContext db) =>
{
    var competitions = await db.Competitions.ToListAsync();
    return competitions.Select(c => c.ToDto()).ToList();
});

app.MapGet("/api/competitions/{id}", async (int id, CalcioDbContext db) =>
{
    var competition = await db.Competitions.FirstOrDefaultAsync(c => c.Id == id);
    return competition is not null ? Results.Ok(competition.ToDto()) : Results.NotFound();
});

app.MapPost("/api/competitions", async (Competition competition, CalcioDbContext db) =>
{
    db.Competitions.Add(competition);
    await db.SaveChangesAsync();
    return Results.Created($"/api/competitions/{competition.Id}", competition);
});

// Upsert competition (find or create)
app.MapPost("/api/competitions/upsert", async (UpsertRequest request, CalcioDbContext db) =>
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
.WithName("UpsertCompetition")
.WithSummary("Find or create competition")
.WithDescription("Finds an existing competition by name, or creates a new one if not found")
.WithTags("Competitions")
.Accepts<UpsertRequest>("application/json")
.Produces<CompetitionDto>(201)
.Produces<CompetitionDto>(200);

// Lookup endpoints for crawler
app.MapGet("/api/lookup/club-id/{externalId}", async (string externalId, CalcioDbContext db) =>
{
    var club = await db.Clubs.FirstOrDefaultAsync(c => c.ExternalId == externalId);
    return club is not null ? Results.Ok(club.Id) : Results.NotFound();
})
.WithName("GetClubIdByExternalId")
.WithSummary("Get club ID by external ID")
.WithTags("Lookup")
.Produces<int>()
.Produces(404);

app.MapGet("/api/lookup/age-group-id/{name}", async (string name, CalcioDbContext db) =>
{
    var ageGroup = await db.AgeGroups.FirstOrDefaultAsync(ag => ag.Name == name);
    return ageGroup is not null ? Results.Ok(ageGroup.Id) : Results.NotFound();
})
.WithName("GetAgeGroupIdByName")
.WithSummary("Get age group ID by name")
.WithTags("Lookup")
.Produces<int>()
.Produces(404);

app.MapGet("/api/lookup/competition-id/{name}", async (string name, CalcioDbContext db) =>
{
    var competition = await db.Competitions.FirstOrDefaultAsync(c => c.Name == name);
    return competition is not null ? Results.Ok(competition.Id) : Results.NotFound();
})
.WithName("GetCompetitionIdByName")
.WithSummary("Get competition ID by name")
.WithTags("Lookup")
.Produces<int>()
.Produces(404);

app.MapGet("/api/lookup/venue-id/{address}", async (string address, CalcioDbContext db) =>
{
    var venue = await db.Venues.FirstOrDefaultAsync(v => v.Address == address);
    return venue is not null ? Results.Ok(venue.Id) : Results.NotFound();
})
.WithName("GetVenueIdByAddress")
.WithSummary("Get venue ID by address")
.WithTags("Lookup")
.Produces<int>()
.Produces(404);

// Matches endpoints
app.MapGet("/api/matches", async (CalcioDbContext db) =>
{
    var matches = await db.Matches
        .Include(m => m.HomeTeam).ThenInclude(t => t!.Club)
        .Include(m => m.AwayTeam).ThenInclude(t => t!.Club)
        .Include(m => m.Venue)
        .Include(m => m.AgeGroup)
        .Include(m => m.Competition)
        .ToListAsync();
    return matches.Select(m => m.ToDto()).ToList();
});

app.MapGet("/api/matches/{id}", async (int id, CalcioDbContext db) =>
{
    var match = await db.Matches
        .Include(m => m.HomeTeam).ThenInclude(t => t!.Club)
        .Include(m => m.AwayTeam).ThenInclude(t => t!.Club)
        .Include(m => m.Venue)
        .Include(m => m.AgeGroup)
        .Include(m => m.Competition)
        .FirstOrDefaultAsync(m => m.Id == id);
    return match is not null ? Results.Ok(match.ToDto()) : Results.NotFound();
});

app.MapPost("/api/matches", async (Match match, CalcioDbContext db) =>
{
    db.Matches.Add(match);
    await db.SaveChangesAsync();
    return Results.Created($"/api/matches/{match.Id}", match);
});

// Search endpoints
app.MapGet("/api/matches/by-team/{teamId}", async (int teamId, CalcioDbContext db) =>
{
    var matches = await db.Matches
        .Include(m => m.HomeTeam).ThenInclude(t => t!.Club)
        .Include(m => m.AwayTeam).ThenInclude(t => t!.Club)
        .Include(m => m.Venue)
        .Include(m => m.AgeGroup)
        .Include(m => m.Competition)
        .Where(m => m.HomeTeamId == teamId || m.AwayTeamId == teamId)
        .ToListAsync();
    return matches.Select(m => m.ToDto()).ToList();
});

app.MapGet("/api/matches/by-venue/{venueId}", async (int venueId, CalcioDbContext db) =>
{
    var matches = await db.Matches
        .Include(m => m.HomeTeam).ThenInclude(t => t!.Club)
        .Include(m => m.AwayTeam).ThenInclude(t => t!.Club)
        .Include(m => m.Venue)
        .Include(m => m.AgeGroup)
        .Include(m => m.Competition)
        .Where(m => m.VenueId == venueId)
        .ToListAsync();
    return matches.Select(m => m.ToDto()).ToList();
});

app.MapGet("/", () => "Calcio API is running!");

app.Run();
