using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Api.Data;
using Api.Models;
using Api.DTOs;
using Api.Extensions;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Tags("Venues")]
public class VenuesController : ControllerBase
{
    private readonly CalcioDbContext _context;

    public VenuesController(CalcioDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Find or create venue by address
    /// </summary>
    /// <param name="request">Venue creation/find request</param>
    /// <returns>The found or created venue</returns>
    [HttpPost("find-or-create")]
    [ProducesResponseType(typeof(VenueDto), 200)]
    [ProducesResponseType(typeof(VenueDto), 201)]
    public async Task<ActionResult<VenueDto>> FindOrCreateVenue([FromBody] CreateVenueRequest request)
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
            return Ok(existingVenue.ToDto());
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
        return CreatedAtAction(nameof(FindOrCreateVenue), new { id = venue.Id }, venue.ToDto());
    }
}
