using Microsoft.EntityFrameworkCore;
using Api.Business.Interfaces;
using Api.Data;
using Calcio.Api.Core.DTOs;
using Api.DTOs.Requests;
using Api.Extensions;
using Api.Models;

namespace Api.Business.Services;

public class VenueService : IVenueService
{
    private readonly CalcioDbContext _context;

    public VenueService(CalcioDbContext context)
    {
        _context = context;
    }

    public async Task<VenueDto> FindOrCreateVenueAsync(CreateVenueRequestDto request)
    {
        // Check if venue already exists by address
        var existingVenue = await _context.Venues.FirstOrDefaultAsync(v => v.Address == request.Address);
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
                await _context.SaveChangesAsync();
            }
            return existingVenue.ToDto();
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

        _context.Venues.Add(venue);
        await _context.SaveChangesAsync();
        return venue.ToDto();
    }

    public async Task<int?> FindVenueIdAsync(string address)
    {
        var venue = await _context.Venues.FirstOrDefaultAsync(v => v.Address == address);
        return venue?.Id;
    }

    public async Task<VenueDto?> UpdateVenueAsync(int id, UpdateVenueDto request)
    {
        var venue = await _context.Venues.FirstOrDefaultAsync(v => v.Id == id);
        if (venue == null)
        {
            return null;
        }

        venue.Location = new NetTopologySuite.Geometries.Point(request.Longitude, request.Latitude)
        {
            SRID = 4326
        };

        await _context.SaveChangesAsync();
        return venue.ToDto();
    }
}
