using System.Net;
using System.Net.Http.Json;
using Calcio.Api.Core.DTOs;
using Api.DTOs.Requests;
using Microsoft.Extensions.DependencyInjection;
using Api.Data;

namespace Api.IntegrationTests.Tests;

public class ClubsControllerTests : IClassFixture<CalcioWebApplicationFactory>
{
    private readonly CalcioWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ClubsControllerTests(CalcioWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetClubs_WithoutFilter_ReturnsAllClubs()
    {
        // Arrange
        await SeedTestData();

        // Act
        var response = await _client.GetAsync("/api/clubs");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var clubs = await response.Content.ReadFromJsonAsync<List<ClubDto>>();
        Assert.NotNull(clubs);
        Assert.True(clubs.Count >= 2);
    }

    [Fact]
    public async Task GetClubs_WithPostCodeFilter_ReturnsFilteredClubs()
    {
        // Arrange
        await SeedTestData();

        // Act
        var response = await _client.GetAsync("/api/clubs?PostCodes=99999");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var clubs = await response.Content.ReadFromJsonAsync<List<ClubDto>>();
        Assert.NotNull(clubs);
        Assert.Single(clubs);
        Assert.Equal("Test Club 2", clubs[0].Name);
    }

    [Fact]
    public async Task FindOrCreateClub_WithValidRequest_ReturnsClub()
    {
        // Arrange
        var request = new FindOrCreateClubRequestDto
        {
            ExternalId = "EXT123",
            Name = "New Test Club",
            PostCode = "54321"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/clubs/find-or-create", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var club = await response.Content.ReadFromJsonAsync<ClubDto>();
        Assert.NotNull(club);
        Assert.Equal(request.Name, club.Name);
        Assert.Equal(request.PostCode, club.PostCode);
        Assert.True(club.Id > 0);
    }

    [Fact]
    public async Task FindOrCreateClub_WithExistingExternalId_ReturnsSameClub()
    {
        // Arrange
        var request = new FindOrCreateClubRequestDto
        {
            ExternalId = "EXT456",
            Name = "Duplicate Test Club",
            PostCode = "99999"
        };

        // Act
        var response1 = await _client.PostAsJsonAsync("/api/clubs/find-or-create", request);
        var club1 = await response1.Content.ReadFromJsonAsync<ClubDto>();

        // Try to create again with same external ID
        var request2 = new FindOrCreateClubRequestDto
        {
            ExternalId = "EXT456",
            Name = "Different Name",
            PostCode = "11111"
        };
        var response2 = await _client.PostAsJsonAsync("/api/clubs/find-or-create", request2);
        var club2 = await response2.Content.ReadFromJsonAsync<ClubDto>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
        Assert.NotNull(club1);
        Assert.NotNull(club2);
        Assert.Equal(club1.Id, club2.Id);
        Assert.Equal(club1.Name, club2.Name);
    }

    [Fact]
    public async Task FindClubId_WithExistingExternalId_ReturnsClubId()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CalcioDbContext>();

        var club = new Api.Models.Club
        {
            Name = "Find Test Club",
            ExternalId = "FIND123",
            PostCode = "12345"
        };
        context.Clubs.Add(club);
        await context.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync($"/api/clubs/find/{club.ExternalId}/id");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var clubId = await response.Content.ReadFromJsonAsync<int>();
        Assert.Equal(club.Id, clubId);
    }

    [Fact]
    public async Task FindClubId_WithNonExistentExternalId_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/clubs/find/NonExistentExternalId/id");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task SeedTestData()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CalcioDbContext>();

        // Only add test clubs if they don't already exist
        if (!context.Clubs.Any(c => c.ExternalId == "TC1" || c.ExternalId == "TC2"))
        {
            var clubs = new[]
            {
                new Api.Models.Club { Name = "Test Club 1", ExternalId = "TC1", PostCode = "01234" },
                new Api.Models.Club { Name = "Test Club 2", ExternalId = "TC2", PostCode = "99999" }
            };

            context.Clubs.AddRange(clubs);
            await context.SaveChangesAsync();
        }
    }
}
