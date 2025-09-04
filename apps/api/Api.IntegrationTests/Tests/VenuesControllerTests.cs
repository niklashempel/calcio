using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Api.Data;
using Api.DTOs.Requests;
using Calcio.Api.Core.DTOs;
using NetTopologySuite.Geometries;
using Api.IntegrationTests.Setup;

namespace Api.IntegrationTests.Tests;

public class VenuesControllerTests : IClassFixture<CalcioWebApplicationFactory>
{
    private readonly CalcioWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public VenuesControllerTests(CalcioWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task FindOrCreateVenue_WithValidRequest_ReturnsVenue()
    {
        // Arrange
        var request = new CreateVenueRequestDto
        {
            Address = "Test Street 123",
            Latitude = 51.0504,
            Longitude = 13.7373
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/venues/find-or-create", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var venue = await response.Content.ReadFromJsonAsync<VenueDto>();
        Assert.NotNull(venue);
        Assert.Equal(request.Address, venue.Address);
        Assert.True(venue.Id > 0);
    }

    [Fact]
    public async Task FindVenueId_WithExistingAddress_ReturnsVenueId()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CalcioDbContext>();

        var geometryFactory = new GeometryFactory(new PrecisionModel(), 4326);
        var location = geometryFactory.CreatePoint(new Coordinate(13.7373, 51.0504));

        var venue = new Models.Venue
        {
            Address = "Integration Test Address",
            Location = location
        };

        context.Venues.Add(venue);
        await context.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync($"/api/venues/find/by-address/{Uri.EscapeDataString(venue.Address)}/id");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var venueId = await response.Content.ReadFromJsonAsync<int>();
        Assert.Equal(venue.Id, venueId);
    }

    [Fact]
    public async Task FindVenueId_WithNonExistentAddress_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/venues/find/by-address/NonExistentAddress/id");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateVenue_WithValidData_ReturnsUpdatedVenue()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CalcioDbContext>();

        var geometryFactory = new GeometryFactory(new PrecisionModel(), 4326);
        var location = geometryFactory.CreatePoint(new Coordinate(13.7373, 51.0504));

        var venue = new Models.Venue
        {
            Address = "Original Address",
            Location = location
        };

        context.Venues.Add(venue);
        await context.SaveChangesAsync();

        var updateRequest = new UpdateVenueDto
        {
            Latitude = 51.1,
            Longitude = 13.8
        };

        // Act
        var response = await _client.PatchAsJsonAsync($"/api/venues/{venue.Id}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updatedVenue = await response.Content.ReadFromJsonAsync<VenueDto>();
        Assert.NotNull(updatedVenue);
        Assert.Equal(updateRequest.Latitude, updatedVenue.Latitude);
        Assert.Equal(updateRequest.Longitude, updatedVenue.Longitude);
        Assert.Equal(venue.Id, updatedVenue.Id);
    }
}
