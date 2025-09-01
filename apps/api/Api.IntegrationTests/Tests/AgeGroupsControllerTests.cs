using System.Net;
using System.Net.Http.Json;
using Calcio.Api.Core.DTOs;
using Api.DTOs.Requests;
using Microsoft.Extensions.DependencyInjection;
using Api.Data;

namespace Api.IntegrationTests.Tests;

public class AgeGroupsControllerTests : IClassFixture<CalcioWebApplicationFactory>
{
    private readonly CalcioWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AgeGroupsControllerTests(CalcioWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task FindOrCreateAgeGroup_WithValidRequest_ReturnsAgeGroup()
    {
        // Arrange
        var request = new FindOrCreateAgeGroupRequestDto
        {
            Name = "U16"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/age-groups/find-or-create", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var ageGroup = await response.Content.ReadFromJsonAsync<AgeGroupDto>();
        Assert.NotNull(ageGroup);
        Assert.Equal(request.Name, ageGroup.Name);
        Assert.True(ageGroup.Id > 0);
    }

    [Fact]
    public async Task FindOrCreateAgeGroup_WithExistingName_ReturnsSameAgeGroup()
    {
        // Arrange
        var request = new FindOrCreateAgeGroupRequestDto
        {
            Name = "U18"
        };

        // Act
        // Create first time
        var response1 = await _client.PostAsJsonAsync("/api/age-groups/find-or-create", request);
        var ageGroup1 = await response1.Content.ReadFromJsonAsync<AgeGroupDto>();

        // Try to create again
        var response2 = await _client.PostAsJsonAsync("/api/age-groups/find-or-create", request);
        var ageGroup2 = await response2.Content.ReadFromJsonAsync<AgeGroupDto>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
        Assert.NotNull(ageGroup1);
        Assert.NotNull(ageGroup2);
        Assert.Equal(ageGroup1.Id, ageGroup2.Id);
        Assert.Equal(ageGroup1.Name, ageGroup2.Name);
    }

    [Fact]
    public async Task FindAgeGroupId_WithExistingName_ReturnsAgeGroupId()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CalcioDbContext>();

        var ageGroup = new Api.Models.AgeGroup { Name = "U21" };
        context.AgeGroups.Add(ageGroup);
        await context.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync($"/api/age-groups/find/{ageGroup.Name}/id");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var ageGroupId = await response.Content.ReadFromJsonAsync<int>();
        Assert.Equal(ageGroup.Id, ageGroupId);
    }

    [Fact]
    public async Task FindAgeGroupId_WithNonExistentName_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/age-groups/find/NonExistentAgeGroup/id");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
