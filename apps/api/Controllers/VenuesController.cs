using Microsoft.AspNetCore.Mvc;
using Api.DTOs;
using Api.Business.Interfaces;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Tags("Venues")]
public class VenuesController : ControllerBase
{
    private readonly IVenueService _venueService;

    public VenuesController(IVenueService venueService)
    {
        _venueService = venueService;
    }

    /// <summary>
    /// Find or create venue by address
    /// </summary>
    /// <param name="request">Venue creation/find request</param>
    /// <returns>The found or created venue</returns>
    [HttpPost("find-or-create")]
    [ProducesResponseType(typeof(VenueDto), 200)]
    [ProducesResponseType(typeof(VenueDto), 201)]
    public async Task<ActionResult<VenueDto>> FindOrCreateVenue([FromBody] CreateVenueRequestDto request)
    {
        var venue = await _venueService.FindOrCreateVenueAsync(request);
        return Ok(venue);
    }

    /// <summary>
    /// Find venue by address and return only the ID
    /// </summary>
    /// <param name="address">Address of the venue</param>
    /// <returns>Venue ID</returns>
    [HttpGet("find/by-address/{address}/id")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<int>> FindVenueId(string address)
    {
        var venueId = await _venueService.FindVenueIdAsync(address);
        return venueId.HasValue ? Ok(venueId.Value) : NotFound();
    }
}
