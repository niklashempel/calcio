using System.Net;
using System.Net.Http.Json;
using Calcio.Api.Core.DTOs;
using Api.DTOs.Requests;
using Microsoft.Extensions.DependencyInjection;
using Api.Data;
using Api.Models;

namespace Api.IntegrationTests.Tests;

public class CompetitionsControllerTests : IClassFixture<CalcioWebApplicationFactory>
{
    private readonly CalcioWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public CompetitionsControllerTests(CalcioWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task FindOrCreateCompetition_WithValidRequest_ReturnsCompetition()
    {
        // Arrange
        var request = new FindOrCreateCompetitionRequestDto
        {
            Id = 1001,
            Name = "Test Championship"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/competitions/find-or-create", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var competition = await response.Content.ReadFromJsonAsync<CompetitionDto>();
        Assert.NotNull(competition);
        Assert.Equal(request.Name, competition.Name);
        Assert.True(competition.Id > 0);
    }

    [Fact]
    public async Task FindOrCreateCompetition_WithExistingName_ReturnsSameCompetition()
    {
        // Arrange
        var request = new FindOrCreateCompetitionRequestDto
        {
            Id = 1003,
            Name = "My Competition"
        };

        // Act
        var response1 = await _client.PostAsJsonAsync("/api/competitions/find-or-create", request);
        var competition1 = await response1.Content.ReadFromJsonAsync<CompetitionDto>();

        // Try to create again with different ID but same name
        var request2 = new FindOrCreateCompetitionRequestDto
        {
            Id = 9999,
            Name = "My Competition"
        };
        var response2 = await _client.PostAsJsonAsync("/api/competitions/find-or-create", request2);
        var competition2 = await response2.Content.ReadFromJsonAsync<CompetitionDto>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
        Assert.NotNull(competition1);
        Assert.NotNull(competition2);
        Assert.Equal(competition1.Id, competition2.Id);
        Assert.Equal(competition1.Name, competition2.Name);
    }

    [Fact]
    public async Task FindCompetitionId_WithExistingName_ReturnsCompetitionId()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CalcioDbContext>();

        var competition = new Competition
        {
            Name = "Find Test Competition"
        };
        context.Competitions.Add(competition);
        await context.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync($"/api/competitions/find/{Uri.EscapeDataString(competition.Name)}/id");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var competitionId = await response.Content.ReadFromJsonAsync<int>();
        Assert.Equal(competition.Id, competitionId);
    }

    [Fact]
    public async Task FindCompetitionId_WithNonExistentName_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/competitions/find/NonExistentCompetition/id");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
