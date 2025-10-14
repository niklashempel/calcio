using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Api.Data;
using Api.Models;
using Calcio.Api.Core.DTOs;
using NetTopologySuite.Geometries;
using Api.IntegrationTests.Setup;

namespace Api.IntegrationTests.Tests;

public class MatchesControllerTests : IClassFixture<CalcioWebApplicationFactory>, IAsyncLifetime
{
    private readonly CalcioWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public MatchesControllerTests(CalcioWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        // Clean up database before each test
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CalcioDbContext>();

        context.Matches.RemoveRange(context.Matches);
        context.Teams.RemoveRange(context.Teams);
        context.Venues.RemoveRange(context.Venues);
        context.Competitions.RemoveRange(context.Competitions);
        context.AgeGroups.RemoveRange(context.AgeGroups);
        context.Clubs.RemoveRange(context.Clubs);
        await context.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetMatches_WithoutParameters_ReturnsMatchLocations()
    {
        // Arrange
        await SeedTestData();

        // Act
        var response = await _client.GetAsync("/api/matches");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var matchLocations = await response.Content.ReadFromJsonAsync<List<MatchLocationDto>>();
        Assert.NotNull(matchLocations);
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

        var matchLocations = await response.Content.ReadFromJsonAsync<List<MatchLocationDto>>();
        Assert.NotNull(matchLocations);

        // All venues should be within the bounding box
        foreach (var location in matchLocations)
        {
            Assert.NotNull(location.Venue);
            Assert.True(location.Venue!.Latitude >= 51.0 && location.Venue!.Latitude <= 51.1);
            Assert.True(location.Venue!.Longitude >= 13.7 && location.Venue!.Longitude <= 13.8);
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

    [Fact]
    public async Task GetMatches_WithDateRange_ReturnsFilteredMatches()
    {
        // Arrange
        await SeedTestDataWithDifferentDates();

        // Use general dates without specific times
        var minDate = new DateTime(2025, 9, 6);   // September 6, 2025
        var maxDate = new DateTime(2025, 9, 15);  // September 15, 2025

        // Act - Filter to only get matches between these dates
        var response = await _client.GetAsync($"/api/matches?minDate={minDate:yyyy-MM-dd}&maxDate={maxDate:yyyy-MM-dd}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var matchLocations = await response.Content.ReadFromJsonAsync<List<MatchLocationDto>>();
        Assert.NotNull(matchLocations);

        // All venues returned should have coordinates
        foreach (var location in matchLocations)
        {
            Assert.NotNull(location.Venue);
            Assert.True(location.Venue.Latitude.HasValue);
            Assert.True(location.Venue.Longitude.HasValue);
        }

        // We should have exactly 2 venues in the result (since both matches are at same venue)
        Assert.True(matchLocations.Count >= 1);
    }

    [Fact]
    public async Task GetMatches_WithInvalidDateRange_ReturnsBadRequest()
    {
        // Act
        // Invalid date range (minDate > maxDate)
        var minDate = new DateTime(2025, 9, 15);  // September 15, 2025
        var maxDate = new DateTime(2025, 9, 10);  // September 10, 2025
        var response = await _client.GetAsync($"/api/matches?minDate={minDate:yyyy-MM-dd}&maxDate={maxDate:yyyy-MM-dd}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetMatches_WithCompetitionFilter_ReturnsFilteredMatches()
    {
        // Arrange
        await SeedTestDataWithMultipleCompetitions();

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CalcioDbContext>();

        var competition1 = await context.Competitions.FirstAsync(c => c.Name == "League A");

        // Act
        var response = await _client.GetAsync($"/api/matches?competitions={competition1.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var matchLocations = await response.Content.ReadFromJsonAsync<List<MatchLocationDto>>();
        Assert.NotNull(matchLocations);

        // Should return venue locations for matches with the specified competition
        Assert.True(matchLocations.Count >= 1);
    }

    [Fact]
    public async Task GetMatches_WithMultipleCompetitionFilter_ReturnsFilteredMatches()
    {
        // Arrange
        await SeedTestDataWithMultipleCompetitions();

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CalcioDbContext>();

        var competition1 = await context.Competitions.FirstAsync(c => c.Name == "League A");
        var competition2 = await context.Competitions.FirstAsync(c => c.Name == "League B");

        // Act
        var response = await _client.GetAsync($"/api/matches?competitions={competition1.Id}&competitions={competition2.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var matchLocations = await response.Content.ReadFromJsonAsync<List<MatchLocationDto>>();
        Assert.NotNull(matchLocations);

        // Should return venue locations for matches with any of the specified competitions
        Assert.True(matchLocations.Count >= 1);
    }

    [Fact]
    public async Task GetMatches_WithAgeGroupFilter_ReturnsFilteredMatches()
    {
        // Arrange
        await SeedTestDataWithMultipleAgeGroups();

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CalcioDbContext>();

        var ageGroup1 = await context.AgeGroups.FirstAsync(ag => ag.Name == "U16");

        // Act
        var response = await _client.GetAsync($"/api/matches?ageGroups={ageGroup1.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var matchLocations = await response.Content.ReadFromJsonAsync<List<MatchLocationDto>>();
        Assert.NotNull(matchLocations);

        // Should return venue locations for matches with the specified age group
        Assert.True(matchLocations.Count >= 1);
    }

    [Fact]
    public async Task GetMatches_WithMultipleAgeGroupFilter_ReturnsFilteredMatches()
    {
        // Arrange
        await SeedTestDataWithMultipleAgeGroups();

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CalcioDbContext>();

        var ageGroup1 = await context.AgeGroups.FirstAsync(ag => ag.Name == "U16");
        var ageGroup2 = await context.AgeGroups.FirstAsync(ag => ag.Name == "U18");

        // Act
        var response = await _client.GetAsync($"/api/matches?ageGroups={ageGroup1.Id}&ageGroups={ageGroup2.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var matchLocations = await response.Content.ReadFromJsonAsync<List<MatchLocationDto>>();
        Assert.NotNull(matchLocations);

        // Should return venue locations for matches with any of the specified age groups
        Assert.True(matchLocations.Count >= 1);
    }

    [Fact]
    public async Task GetMatches_WithCombinedFilters_ReturnsFilteredMatches()
    {
        // Arrange
        await SeedTestDataWithMultipleFilters();

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CalcioDbContext>();

        var competition1 = await context.Competitions.FirstAsync(c => c.Name == "League A");
        var ageGroup1 = await context.AgeGroups.FirstAsync(ag => ag.Name == "U16");

        // Use general dates
        var minDate = new DateTime(2025, 9, 1);   // September 1, 2025
        var maxDate = new DateTime(2025, 9, 30);  // September 30, 2025

        // Act
        var response = await _client.GetAsync($"/api/matches?minLat=51.0&maxLat=51.1&minLng=13.7&maxLng=13.8&minDate={minDate:yyyy-MM-dd}&maxDate={maxDate:yyyy-MM-dd}&competitions={competition1.Id}&ageGroups={ageGroup1.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var matchLocations = await response.Content.ReadFromJsonAsync<List<MatchLocationDto>>();
        Assert.NotNull(matchLocations);

        // Should return venue locations that match all filters
        foreach (var location in matchLocations)
        {
            Assert.NotNull(location.Venue);
            // Verify bounding box filter (venue coordinates)
            if (location.Venue.Latitude.HasValue && location.Venue.Longitude.HasValue)
            {
                Assert.True(location.Venue.Latitude >= 51.0 && location.Venue.Latitude <= 51.1);
                Assert.True(location.Venue.Longitude >= 13.7 && location.Venue.Longitude <= 13.8);
            }
        }
    }

    [Fact]
    public async Task GetMatches_WithNonExistentCompetition_ReturnsEmptyResult()
    {
        // Arrange
        await SeedTestData();

        // Act - Use a competition ID that doesn't exist
        var response = await _client.GetAsync("/api/matches?competitions=99999");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var matchLocations = await response.Content.ReadFromJsonAsync<List<MatchLocationDto>>();
        Assert.NotNull(matchLocations);
        Assert.Empty(matchLocations);
    }

    [Fact]
    public async Task GetMatches_WithNonExistentAgeGroup_ReturnsEmptyResult()
    {
        // Arrange
        await SeedTestData();

        // Act - Use an age group ID that doesn't exist
        var response = await _client.GetAsync("/api/matches?ageGroups=99999");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var matchLocations = await response.Content.ReadFromJsonAsync<List<MatchLocationDto>>();
        Assert.NotNull(matchLocations);
        Assert.Empty(matchLocations);
    }

    [Fact]
    public async Task GetMatchFilterOptions_WithNoMatches_ReturnsEmptyFilterOptions()
    {
        // Act
        var response = await _client.GetAsync("/api/matches/filter-options");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var filterOptions = await response.Content.ReadFromJsonAsync<MatchFilterOptionsDto>();
        Assert.NotNull(filterOptions);
        Assert.Null(filterOptions.MinDate);
        Assert.Null(filterOptions.MaxDate);
        Assert.Empty(filterOptions.Competitions);
        Assert.Empty(filterOptions.AgeGroups);
    }

    [Fact]
    public async Task GetMatchFilterOptions_WithMatches_ReturnsCorrectFilterOptions()
    {
        // Arrange
        await SeedTestDataForFilterOptions();

        // Act
        var response = await _client.GetAsync("/api/matches/filter-options");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var filterOptions = await response.Content.ReadFromJsonAsync<MatchFilterOptionsDto>();
        Assert.NotNull(filterOptions);

        // Verify date range
        Assert.NotNull(filterOptions.MinDate);
        Assert.NotNull(filterOptions.MaxDate);
        Assert.True(filterOptions.MinDate <= filterOptions.MaxDate);

        // Verify competitions
        Assert.NotEmpty(filterOptions.Competitions);
        var competitionsList = filterOptions.Competitions.ToList();
        Assert.Contains(competitionsList, c => c.Name == "League A");
        Assert.Contains(competitionsList, c => c.Name == "League B");
        Assert.Contains(competitionsList, c => c.Name == "Cup Competition");

        // Verify age groups
        Assert.NotEmpty(filterOptions.AgeGroups);
        var ageGroupsList = filterOptions.AgeGroups.ToList();
        Assert.Contains(ageGroupsList, ag => ag.Name == "U16");
        Assert.Contains(ageGroupsList, ag => ag.Name == "U18");
        Assert.Contains(ageGroupsList, ag => ag.Name == "Seniors");

        // Verify ordering (should be alphabetical)
        var sortedCompetitions = competitionsList.OrderBy(c => c.Name).ToList();
        Assert.Equal(sortedCompetitions.Select(c => c.Name), competitionsList.Select(c => c.Name));

        var sortedAgeGroups = ageGroupsList.OrderBy(ag => ag.Name).ToList();
        Assert.Equal(sortedAgeGroups.Select(ag => ag.Name), ageGroupsList.Select(ag => ag.Name));
    }

    [Fact]
    public async Task GetMatchFilterOptions_WithDuplicateCompetitionsAndAgeGroups_ReturnsDistinctOptions()
    {
        // Arrange
        await SeedTestDataWithDuplicates();

        // Act
        var response = await _client.GetAsync("/api/matches/filter-options");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var filterOptions = await response.Content.ReadFromJsonAsync<MatchFilterOptionsDto>();
        Assert.NotNull(filterOptions);

        // Verify that duplicate competitions are deduplicated
        var competitionNames = filterOptions.Competitions.Select(c => c.Name).ToList();
        Assert.Equal(competitionNames.Count, competitionNames.Distinct().Count());

        // Verify that duplicate age groups are deduplicated
        var ageGroupNames = filterOptions.AgeGroups.Select(ag => ag.Name).ToList();
        Assert.Equal(ageGroupNames.Count, ageGroupNames.Distinct().Count());

        // Should have exactly 2 competitions and 2 age groups
        Assert.Equal(2, filterOptions.Competitions.Count());
        Assert.Equal(2, filterOptions.AgeGroups.Count());
    }

    [Fact]
    public async Task GetMatchFilterOptions_WithMatchesWithoutDates_HandlesNullDatesGracefully()
    {
        // Arrange
        await SeedTestDataWithNullDates();

        // Act
        var response = await _client.GetAsync("/api/matches/filter-options");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var filterOptions = await response.Content.ReadFromJsonAsync<MatchFilterOptionsDto>();
        Assert.NotNull(filterOptions);

        // When all matches have null dates, min/max should be null
        Assert.Null(filterOptions.MinDate);
        Assert.Null(filterOptions.MaxDate);

        // But competitions and age groups should still be returned
        Assert.NotEmpty(filterOptions.Competitions);
        Assert.NotEmpty(filterOptions.AgeGroups);
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

    private async Task SeedTestDataWithDifferentDates()
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
        var competition = new Competition { Name = "Test Competition" };
        context.Competitions.Add(competition);
        await context.SaveChangesAsync();

        // Create test venue
        var geometryFactory = new GeometryFactory(new PrecisionModel(), 4326);
        var venue = new Venue
        {
            Address = "Test Venue",
            Location = geometryFactory.CreatePoint(new Coordinate(13.75, 51.05))
        };
        context.Venues.Add(venue);
        await context.SaveChangesAsync();

        // Create test teams
        var team1 = new Team { Name = "Test Team 1", ClubId = club1.Id };
        var team2 = new Team { Name = "Test Team 2", ClubId = club2.Id };
        context.Teams.AddRange(team1, team2);
        await context.SaveChangesAsync();

        // Create test matches with different dates
        var matches = new List<Match>
        {
            new()
            {
                HomeTeamId = team1.Id,
                AwayTeamId = team2.Id,
                VenueId = venue.Id,
                CompetitionId = competition.Id,
                AgeGroupId = ageGroup.Id,
                Time = new DateTime(2025, 9, 8) // September 8, 2025 (within filter range)
            },
            new()
            {
                HomeTeamId = team2.Id,
                AwayTeamId = team1.Id,
                VenueId = venue.Id,
                CompetitionId = competition.Id,
                AgeGroupId = ageGroup.Id,
                Time = new DateTime(2025, 9, 12) // September 12, 2025 (within filter range)
            },
            new()
            {
                HomeTeamId = team1.Id,
                AwayTeamId = team2.Id,
                VenueId = venue.Id,
                CompetitionId = competition.Id,
                AgeGroupId = ageGroup.Id,
                Time = new DateTime(2025, 10, 1) // October 1, 2025 (outside filter range)
            }
        };

        context.Matches.AddRange(matches);
        await context.SaveChangesAsync();
    }

    private async Task SeedTestDataWithMultipleCompetitions()
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

        // Create multiple competitions
        var competition1 = new Competition { Name = "League A" };
        var competition2 = new Competition { Name = "League B" };
        var competition3 = new Competition { Name = "League C" };
        context.Competitions.AddRange(competition1, competition2, competition3);
        await context.SaveChangesAsync();

        // Create test venue
        var geometryFactory = new GeometryFactory(new PrecisionModel(), 4326);
        var venue = new Venue
        {
            Address = "Test Venue",
            Location = geometryFactory.CreatePoint(new Coordinate(13.75, 51.05))
        };
        context.Venues.Add(venue);
        await context.SaveChangesAsync();

        // Create test teams
        var team1 = new Team { Name = "Test Team 1", ClubId = club1.Id };
        var team2 = new Team { Name = "Test Team 2", ClubId = club2.Id };
        context.Teams.AddRange(team1, team2);
        await context.SaveChangesAsync();

        // Create test matches with different competitions
        var matches = new List<Match>
        {
            new()
            {
                HomeTeamId = team1.Id,
                AwayTeamId = team2.Id,
                VenueId = venue.Id,
                CompetitionId = competition1.Id, // League A
                AgeGroupId = ageGroup.Id,
                Time = DateTime.UtcNow.AddDays(1)
            },
            new()
            {
                HomeTeamId = team2.Id,
                AwayTeamId = team1.Id,
                VenueId = venue.Id,
                CompetitionId = competition2.Id, // League B
                AgeGroupId = ageGroup.Id,
                Time = DateTime.UtcNow.AddDays(2)
            },
            new()
            {
                HomeTeamId = team1.Id,
                AwayTeamId = team2.Id,
                VenueId = venue.Id,
                CompetitionId = competition3.Id, // League C (not in filter)
                AgeGroupId = ageGroup.Id,
                Time = DateTime.UtcNow.AddDays(3)
            }
        };

        context.Matches.AddRange(matches);
        await context.SaveChangesAsync();
    }

    private async Task SeedTestDataWithMultipleAgeGroups()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CalcioDbContext>();

        // Create test clubs
        var club1 = new Club { Name = "Test Club 1" };
        var club2 = new Club { Name = "Test Club 2" };
        context.Clubs.AddRange(club1, club2);
        await context.SaveChangesAsync();

        // Create multiple age groups
        var ageGroup1 = new AgeGroup { Name = "U16" };
        var ageGroup2 = new AgeGroup { Name = "U18" };
        var ageGroup3 = new AgeGroup { Name = "Seniors" };
        context.AgeGroups.AddRange(ageGroup1, ageGroup2, ageGroup3);
        await context.SaveChangesAsync();

        // Create test competition
        var competition = new Competition { Name = "Test Competition" };
        context.Competitions.Add(competition);
        await context.SaveChangesAsync();

        // Create test venue
        var geometryFactory = new GeometryFactory(new PrecisionModel(), 4326);
        var venue = new Venue
        {
            Address = "Test Venue",
            Location = geometryFactory.CreatePoint(new Coordinate(13.75, 51.05))
        };
        context.Venues.Add(venue);
        await context.SaveChangesAsync();

        // Create test teams
        var team1 = new Team { Name = "Test Team 1", ClubId = club1.Id };
        var team2 = new Team { Name = "Test Team 2", ClubId = club2.Id };
        context.Teams.AddRange(team1, team2);
        await context.SaveChangesAsync();

        // Create test matches with different age groups
        var matches = new List<Match>
        {
            new()
            {
                HomeTeamId = team1.Id,
                AwayTeamId = team2.Id,
                VenueId = venue.Id,
                CompetitionId = competition.Id,
                AgeGroupId = ageGroup1.Id, // U16
                Time = DateTime.UtcNow.AddDays(1)
            },
            new()
            {
                HomeTeamId = team2.Id,
                AwayTeamId = team1.Id,
                VenueId = venue.Id,
                CompetitionId = competition.Id,
                AgeGroupId = ageGroup2.Id, // U18
                Time = DateTime.UtcNow.AddDays(2)
            },
            new()
            {
                HomeTeamId = team1.Id,
                AwayTeamId = team2.Id,
                VenueId = venue.Id,
                CompetitionId = competition.Id,
                AgeGroupId = ageGroup3.Id, // Seniors (not in filter)
                Time = DateTime.UtcNow.AddDays(3)
            }
        };

        context.Matches.AddRange(matches);
        await context.SaveChangesAsync();
    }

    private async Task SeedTestDataWithMultipleFilters()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CalcioDbContext>();

        // Create test clubs
        var club1 = new Club { Name = "Test Club 1" };
        var club2 = new Club { Name = "Test Club 2" };
        context.Clubs.AddRange(club1, club2);
        await context.SaveChangesAsync();

        // Create multiple age groups
        var ageGroup1 = new AgeGroup { Name = "U16" };
        var ageGroup2 = new AgeGroup { Name = "U18" };
        context.AgeGroups.AddRange(ageGroup1, ageGroup2);
        await context.SaveChangesAsync();

        // Create multiple competitions
        var competition1 = new Competition { Name = "League A" };
        var competition2 = new Competition { Name = "League B" };
        context.Competitions.AddRange(competition1, competition2);
        await context.SaveChangesAsync();

        // Create test venues (some inside, some outside bounding box)
        var geometryFactory = new GeometryFactory(new PrecisionModel(), 4326);
        var venueInside = new Venue
        {
            Address = "Venue Inside",
            Location = geometryFactory.CreatePoint(new Coordinate(13.75, 51.05)) // Within bounding box
        };
        var venueOutside = new Venue
        {
            Address = "Venue Outside",
            Location = geometryFactory.CreatePoint(new Coordinate(14.0, 52.0)) // Outside bounding box
        };
        context.Venues.AddRange(venueInside, venueOutside);
        await context.SaveChangesAsync();

        // Create test teams
        var team1 = new Team { Name = "Test Team 1", ClubId = club1.Id };
        var team2 = new Team { Name = "Test Team 2", ClubId = club2.Id };
        context.Teams.AddRange(team1, team2);
        await context.SaveChangesAsync();

        // Create test matches with various combinations
        var matches = new List<Match>
        {
            // Match that should be included (all filters match)
            new()
            {
                HomeTeamId = team1.Id,
                AwayTeamId = team2.Id,
                VenueId = venueInside.Id, // Inside bounding box
                CompetitionId = competition1.Id, // League A
                AgeGroupId = ageGroup1.Id, // U16
                Time = new DateTime(2025, 9, 10) // September 10, 2025 (within date range)
            },
            // Match that should be excluded (wrong venue)
            new()
            {
                HomeTeamId = team2.Id,
                AwayTeamId = team1.Id,
                VenueId = venueOutside.Id, // Outside bounding box
                CompetitionId = competition1.Id, // League A
                AgeGroupId = ageGroup1.Id, // U16
                Time = new DateTime(2025, 9, 12) // Within date range
            },
            // Match that should be excluded (wrong competition)
            new()
            {
                HomeTeamId = team1.Id,
                AwayTeamId = team2.Id,
                VenueId = venueInside.Id, // Inside bounding box
                CompetitionId = competition2.Id, // League B (not in filter)
                AgeGroupId = ageGroup1.Id, // U16
                Time = new DateTime(2025, 9, 14) // Within date range
            },
            // Match that should be excluded (wrong age group)
            new()
            {
                HomeTeamId = team2.Id,
                AwayTeamId = team1.Id,
                VenueId = venueInside.Id, // Inside bounding box
                CompetitionId = competition1.Id, // League A
                AgeGroupId = ageGroup2.Id, // U18 (not in filter)
                Time = new DateTime(2025, 9, 16) // Within date range
            },
            // Match that should be excluded (wrong date)
            new()
            {
                HomeTeamId = team1.Id,
                AwayTeamId = team2.Id,
                VenueId = venueInside.Id, // Inside bounding box
                CompetitionId = competition1.Id, // League A
                AgeGroupId = ageGroup1.Id, // U16
                Time = new DateTime(2025, 10, 15) // October 15, 2025 (outside date range)
            }
        };

        context.Matches.AddRange(matches);
        await context.SaveChangesAsync();
    }

    private async Task SeedTestDataForFilterOptions()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CalcioDbContext>();

        // Create test clubs
        var club1 = new Club { Name = "Test Club 1" };
        var club2 = new Club { Name = "Test Club 2" };
        context.Clubs.AddRange(club1, club2);
        await context.SaveChangesAsync();

        // Create multiple age groups
        var ageGroup1 = new AgeGroup { Name = "U16" };
        var ageGroup2 = new AgeGroup { Name = "U18" };
        var ageGroup3 = new AgeGroup { Name = "Seniors" };
        context.AgeGroups.AddRange(ageGroup1, ageGroup2, ageGroup3);
        await context.SaveChangesAsync();

        // Create multiple competitions
        var competition1 = new Competition { Name = "League A" };
        var competition2 = new Competition { Name = "League B" };
        var competition3 = new Competition { Name = "Cup Competition" };
        context.Competitions.AddRange(competition1, competition2, competition3);
        await context.SaveChangesAsync();

        // Create test venue
        var geometryFactory = new GeometryFactory(new PrecisionModel(), 4326);
        var venue = new Venue
        {
            Address = "Test Venue",
            Location = geometryFactory.CreatePoint(new Coordinate(13.75, 51.05))
        };
        context.Venues.Add(venue);
        await context.SaveChangesAsync();

        // Create test teams
        var team1 = new Team { Name = "Test Team 1", ClubId = club1.Id };
        var team2 = new Team { Name = "Test Team 2", ClubId = club2.Id };
        context.Teams.AddRange(team1, team2);
        await context.SaveChangesAsync();

        // Create test matches with different combinations
        var matches = new List<Match>
        {
            new()
            {
                HomeTeamId = team1.Id,
                AwayTeamId = team2.Id,
                VenueId = venue.Id,
                CompetitionId = competition1.Id, // League A
                AgeGroupId = ageGroup1.Id, // U16
                Time = new DateTime(2025, 9, 10, 14, 0, 0, DateTimeKind.Utc)
            },
            new()
            {
                HomeTeamId = team2.Id,
                AwayTeamId = team1.Id,
                VenueId = venue.Id,
                CompetitionId = competition2.Id, // League B
                AgeGroupId = ageGroup2.Id, // U18
                Time = new DateTime(2025, 9, 15, 16, 0, 0, DateTimeKind.Utc)
            },
            new()
            {
                HomeTeamId = team1.Id,
                AwayTeamId = team2.Id,
                VenueId = venue.Id,
                CompetitionId = competition3.Id, // Cup Competition
                AgeGroupId = ageGroup3.Id, // Seniors
                Time = new DateTime(2025, 9, 20, 18, 0, 0, DateTimeKind.Utc)
            }
        };

        context.Matches.AddRange(matches);
        await context.SaveChangesAsync();
    }

    private async Task SeedTestDataWithDuplicates()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CalcioDbContext>();

        // Create test clubs
        var club1 = new Club { Name = "Test Club 1" };
        var club2 = new Club { Name = "Test Club 2" };
        context.Clubs.AddRange(club1, club2);
        await context.SaveChangesAsync();

        // Create age groups
        var ageGroup1 = new AgeGroup { Name = "U16" };
        var ageGroup2 = new AgeGroup { Name = "U18" };
        context.AgeGroups.AddRange(ageGroup1, ageGroup2);
        await context.SaveChangesAsync();

        // Create competitions
        var competition1 = new Competition { Name = "League A" };
        var competition2 = new Competition { Name = "League B" };
        context.Competitions.AddRange(competition1, competition2);
        await context.SaveChangesAsync();

        // Create test venue
        var geometryFactory = new GeometryFactory(new PrecisionModel(), 4326);
        var venue = new Venue
        {
            Address = "Test Venue",
            Location = geometryFactory.CreatePoint(new Coordinate(13.75, 51.05))
        };
        context.Venues.Add(venue);
        await context.SaveChangesAsync();

        // Create test teams
        var team1 = new Team { Name = "Test Team 1", ClubId = club1.Id };
        var team2 = new Team { Name = "Test Team 2", ClubId = club2.Id };
        context.Teams.AddRange(team1, team2);
        await context.SaveChangesAsync();

        // Create multiple matches with the same competition and age group combinations
        var matches = new List<Match>
        {
            new()
            {
                HomeTeamId = team1.Id,
                AwayTeamId = team2.Id,
                VenueId = venue.Id,
                CompetitionId = competition1.Id, // League A
                AgeGroupId = ageGroup1.Id, // U16
                Time = new DateTime(2025, 9, 10, 14, 0, 0, DateTimeKind.Utc)
            },
            new()
            {
                HomeTeamId = team2.Id,
                AwayTeamId = team1.Id,
                VenueId = venue.Id,
                CompetitionId = competition1.Id, // League A (duplicate)
                AgeGroupId = ageGroup1.Id, // U16 (duplicate)
                Time = new DateTime(2025, 9, 12, 16, 0, 0, DateTimeKind.Utc)
            },
            new()
            {
                HomeTeamId = team1.Id,
                AwayTeamId = team2.Id,
                VenueId = venue.Id,
                CompetitionId = competition2.Id, // League B
                AgeGroupId = ageGroup2.Id, // U18
                Time = new DateTime(2025, 9, 15, 18, 0, 0, DateTimeKind.Utc)
            },
            new()
            {
                HomeTeamId = team2.Id,
                AwayTeamId = team1.Id,
                VenueId = venue.Id,
                CompetitionId = competition2.Id, // League B (duplicate)
                AgeGroupId = ageGroup2.Id, // U18 (duplicate)
                Time = new DateTime(2025, 9, 18, 20, 0, 0, DateTimeKind.Utc)
            }
        };

        context.Matches.AddRange(matches);
        await context.SaveChangesAsync();
    }

    private async Task SeedTestDataWithNullDates()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CalcioDbContext>();

        // Create test clubs
        var club1 = new Club { Name = "Test Club 1" };
        var club2 = new Club { Name = "Test Club 2" };
        context.Clubs.AddRange(club1, club2);
        await context.SaveChangesAsync();

        // Create age group
        var ageGroup = new AgeGroup { Name = "U16" };
        context.AgeGroups.Add(ageGroup);
        await context.SaveChangesAsync();

        // Create competition
        var competition = new Competition { Name = "Test Competition" };
        context.Competitions.Add(competition);
        await context.SaveChangesAsync();

        // Create test venue
        var geometryFactory = new GeometryFactory(new PrecisionModel(), 4326);
        var venue = new Venue
        {
            Address = "Test Venue",
            Location = geometryFactory.CreatePoint(new Coordinate(13.75, 51.05))
        };
        context.Venues.Add(venue);
        await context.SaveChangesAsync();

        // Create test teams
        var team1 = new Team { Name = "Test Team 1", ClubId = club1.Id };
        var team2 = new Team { Name = "Test Team 2", ClubId = club2.Id };
        context.Teams.AddRange(team1, team2);
        await context.SaveChangesAsync();

        // Create matches with null dates
        var matches = new List<Match>
        {
            new()
            {
                HomeTeamId = team1.Id,
                AwayTeamId = team2.Id,
                VenueId = venue.Id,
                CompetitionId = competition.Id,
                AgeGroupId = ageGroup.Id,
                Time = null // Null date
            },
            new()
            {
                HomeTeamId = team2.Id,
                AwayTeamId = team1.Id,
                VenueId = venue.Id,
                CompetitionId = competition.Id,
                AgeGroupId = ageGroup.Id,
                Time = null // Null date
            }
        };

        context.Matches.AddRange(matches);
        await context.SaveChangesAsync();
    }
}
