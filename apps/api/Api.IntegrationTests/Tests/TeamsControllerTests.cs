using System.Net;
using System.Net.Http.Json;
using Calcio.Api.Core.DTOs;
using Api.DTOs.Requests;
using Microsoft.Extensions.DependencyInjection;
using Api.Data;
using Api.Models;
using Api.IntegrationTests.Setup;

namespace Api.IntegrationTests.Tests;

public class TeamsControllerTests : IClassFixture<CalcioWebApplicationFactory>
{
    private readonly CalcioWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public TeamsControllerTests(CalcioWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task FindOrCreateTeam_WithValidRequest_ReturnsTeam()
    {
        // Arrange
        await SeedTestData();

        var request = new FindOrCreateTeamRequestDto
        {
            Name = "Test Team A",
            ClubExternalId = "CLUB001",
            ExternalId = "TEAM001"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/teams/find-or-create", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var team = await response.Content.ReadFromJsonAsync<TeamDto>();
        Assert.NotNull(team);
        Assert.Equal(request.Name, team.Name);
        Assert.True(team.Id > 0);
        Assert.True(team.ClubId > 0);
    }

    [Fact]
    public async Task FindOrCreateTeam_WithExistingExternalId_ReturnsSameTeam()
    {
        // Arrange
        await SeedTestData();

        var request = new FindOrCreateTeamRequestDto
        {
            Name = "Original Team Name",
            ClubExternalId = "CLUB001",
            ExternalId = "TEAM002"
        };

        // Act
        var response1 = await _client.PostAsJsonAsync("/api/teams/find-or-create", request);
        var team1 = await response1.Content.ReadFromJsonAsync<TeamDto>();

        // Try to create again with same external ID
        var request2 = new FindOrCreateTeamRequestDto
        {
            Name = "Different Team Name",
            ClubExternalId = "CLUB002",
            ExternalId = "TEAM002"
        };
        var response2 = await _client.PostAsJsonAsync("/api/teams/find-or-create", request2);
        var team2 = await response2.Content.ReadFromJsonAsync<TeamDto>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
        Assert.NotNull(team1);
        Assert.NotNull(team2);
        Assert.Equal(team1.Id, team2.Id);
        Assert.Equal("Different Team Name", team2.Name);
        Assert.Equal(team1.ClubId, team2.ClubId);
    }

    [Fact]
    public async Task FindOrCreateTeam_WithInvalidClubExternalId_ReturnsBadRequest()
    {
        // Arrange
        var request = new FindOrCreateTeamRequestDto
        {
            Name = "Team Without Club",
            ClubExternalId = "NONEXISTENT_CLUB",
            ExternalId = "TEAM003"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/teams/find-or-create", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task FindTeamId_WithExistingExternalId_ReturnsTeamId()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CalcioDbContext>();

        var club = new Club
        {
            Name = "Test Club for Team",
            ExternalId = "CLUB_FOR_TEAM",
            PostCode = "12345"
        };
        context.Clubs.Add(club);
        await context.SaveChangesAsync();

        var team = new Team
        {
            Name = "Find Test Team",
            ExternalId = "FIND_TEAM_001",
            ClubId = club.Id
        };
        context.Teams.Add(team);
        await context.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync($"/api/teams/find/{team.ExternalId}/id");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var teamId = await response.Content.ReadFromJsonAsync<int>();
        Assert.Equal(team.Id, teamId);
    }

    [Fact]
    public async Task FindTeamId_WithNonExistentExternalId_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/teams/find/NonExistentTeamExternalId/id");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task FindOrCreateTeam_WithoutExternalId_ReturnsTeam()
    {
        // Arrange
        await SeedTestData();

        var request = new FindOrCreateTeamRequestDto
        {
            Name = "Team Without External ID",
            ClubExternalId = "CLUB001",
            ExternalId = null
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/teams/find-or-create", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var team = await response.Content.ReadFromJsonAsync<TeamDto>();
        Assert.NotNull(team);
        Assert.Equal(request.Name, team.Name);
        Assert.True(team.Id > 0);
        Assert.True(team.ClubId > 0);
    }

    private async Task SeedTestData()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CalcioDbContext>();

        // Only add test clubs if they don't already exist
        if (!context.Clubs.Any(c => c.ExternalId == "CLUB001" || c.ExternalId == "CLUB002"))
        {
            var clubs = new[]
            {
                new Club { Name = "Test Club 1", ExternalId = "CLUB001", PostCode = "01234" },
                new Club { Name = "Test Club 2", ExternalId = "CLUB002", PostCode = "56789" }
            };

            context.Clubs.AddRange(clubs);
            await context.SaveChangesAsync();
        }
    }
}
