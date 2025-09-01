using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Api.Data;
using Api.Models;
using Calcio.Api.Core.DTOs;
using NetTopologySuite.Geometries;

namespace Api.IntegrationTests.Tests;

public class MatchesControllerTests : IClassFixture<CalcioWebApplicationFactory>
{
    private readonly CalcioWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public MatchesControllerTests(CalcioWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetMatches_WithoutParameters_ReturnsGroupedMatches()
    {
        // Arrange
        await SeedTestData();

        // Act
        var response = await _client.GetAsync("/api/matches");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var groupedMatches = await response.Content.ReadFromJsonAsync<List<GroupedMatchesByVenueDto>>();
        Assert.NotNull(groupedMatches);
    }

    [Fact]
    public async Task GetMatches_WithBoundingBox_ReturnsFilteredMatches()
    {
        // Arrange
        await SeedTestData();

        // Act
        var response = await _client.GetAsync("/api/matches?minLat=51.0&maxLat=51.1&minLng=13.7&maxLng=13.8");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var groupedMatches = await response.Content.ReadFromJsonAsync<List<GroupedMatchesByVenueDto>>();
        Assert.NotNull(groupedMatches);

        // All venues should be within the bounding box
        foreach (var group in groupedMatches)
        {
            if (group.Venue?.Latitude.HasValue == true && group.Venue?.Longitude.HasValue == true)
            {
                Assert.True(group.Venue.Latitude >= 51.0 && group.Venue.Latitude <= 51.1);
                Assert.True(group.Venue.Longitude >= 13.7 && group.Venue.Longitude <= 13.8);
            }
        }
    }

    [Fact]
    public async Task GetMatches_WithInvalidBoundingBox_ReturnsBadRequest()
    {
        // Act
        // Invalid coordinates (minLat > maxLat)
        var response = await _client.GetAsync("/api/matches?minLat=51.1&maxLat=51.0&minLng=13.7&maxLng=13.8");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task SeedTestData()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CalcioDbContext>();

        // Create test clubs
        var club1 = new Club { Name = "Test Club 1" };
        var club2 = new Club { Name = "Test Club 2" };
        context.Clubs.AddRange(club1, club2);
        await context.SaveChangesAsync();

        // Create test age group
        var ageGroup = new AgeGroup { Name = "Test Age Group" };
        context.AgeGroups.Add(ageGroup);
        await context.SaveChangesAsync();

        // Create test competition
        var competition = new Competition
        {
            Name = "Test Competition"
        };
        context.Competitions.Add(competition);
        await context.SaveChangesAsync();

        // Create test venues using geometry factory
        var geometryFactory = new GeometryFactory(new PrecisionModel(), 4326);
        var venue1 = new Venue
        {
            Address = "My Street 1",
            Location = geometryFactory.CreatePoint(new Coordinate(13.75, 51.05))
        };
        var venue2 = new Venue
        {
            Address = "My Street 2",
            Location = geometryFactory.CreatePoint(new Coordinate(13.76, 51.06))
        };
        context.Venues.AddRange(venue1, venue2);
        await context.SaveChangesAsync();

        // Create test teams
        var team1 = new Team
        {
            Name = "Test Team 1",
            ClubId = club1.Id
        };
        var team2 = new Team
        {
            Name = "Test Team 2",
            ClubId = club2.Id
        };
        context.Teams.AddRange(team1, team2);
        await context.SaveChangesAsync();

        // Create test matches
        var matches = new List<Match>
        {
            new()
            {
                HomeTeamId = team1.Id,
                AwayTeamId = team2.Id,
                VenueId = venue1.Id,
                CompetitionId = competition.Id,
                AgeGroupId = ageGroup.Id,
                Time = DateTime.UtcNow.AddDays(1) // Tomorrow
            },
            new()
            {
                HomeTeamId = team2.Id,
                AwayTeamId = team1.Id,
                VenueId = venue2.Id,
                CompetitionId = competition.Id,
                AgeGroupId = ageGroup.Id,
                Time = DateTime.UtcNow.AddDays(7) // Next week
            }
        };

        context.Matches.AddRange(matches);
        await context.SaveChangesAsync();
    }
}
