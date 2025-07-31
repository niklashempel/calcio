using Microsoft.EntityFrameworkCore;
using Api.Data;
using Api.Models;
using Api.Extensions;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add Entity Framework
builder.Services.AddDbContext<CalcioDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
        o => o.UseNetTopologySuite()));

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
}); var app = builder.Build();

// Use CORS
app.UseCors("AllowAll");

// Clubs endpoints
app.MapGet("/api/clubs", async (CalcioDbContext db) =>
{
    var clubs = await db.Clubs.Include(c => c.Teams).ToListAsync();
    return clubs.Select(c => c.ToDto()).ToList();
});

app.MapGet("/api/clubs/{id}", async (int id, CalcioDbContext db) =>
{
    var club = await db.Clubs.Include(c => c.Teams).FirstOrDefaultAsync(c => c.Id == id);
    return club is not null ? Results.Ok(club.ToDto()) : Results.NotFound();
});

app.MapGet("/api/clubs/external/{externalId}", async (string externalId, CalcioDbContext db) =>
{
    var club = await db.Clubs.Include(c => c.Teams).FirstOrDefaultAsync(c => c.ExternalId == externalId);
    return club is not null ? Results.Ok(club.ToDto()) : Results.NotFound();
});

app.MapPost("/api/clubs", async (Club club, CalcioDbContext db) =>
{
    db.Clubs.Add(club);
    await db.SaveChangesAsync();
    return Results.Created($"/api/clubs/{club.Id}", club);
});

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
