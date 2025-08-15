using Microsoft.AspNetCore.Mvc;
using Api.DTOs;
using Api.Models;
using Api.Business.Interfaces;
using Api.DTOs.Requests;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Tags("Matches")]
public class MatchesController : ControllerBase
{
    private readonly IMatchService _matchService;

    public MatchesController(IMatchService matchService)
    {
        _matchService = matchService;
    }

    /// <summary>
    /// Upsert a new match
    /// </summary>
    /// <param name="request">Match upsert request</param>
    /// <returns>The created or updated match</returns>
    [HttpPost]
    [ProducesResponseType(typeof(MatchDto), 201)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<MatchDto>> UpsertMatch([FromBody] UpsertMatchRequestDto request)
    {
        try
        {
            var match = await _matchService.UpsertMatchAsync(request);
            return Ok(match);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Get matches within a specified bounding box and/or date range
    /// </summary>
    /// <param name="request">Request containing the bounding box coordinates and optional date filters</param>
    /// <returns>List of matches within the specified criteria</returns>
    /// <remarks>
    /// Example URL: GET /api/matches?minLat=52.4&maxLat=52.6&minLng=13.3&maxLng=13.5
    /// 
    /// Bounding box parameters (all optional, but if used, all four must be provided):
    /// - minLat: Minimum latitude (south boundary)
    /// - maxLat: Maximum latitude (north boundary) 
    /// - minLng: Minimum longitude (west boundary)
    /// - maxLng: Maximum longitude (east boundary)
    /// </remarks>
    [HttpGet()]
    [ProducesResponseType(typeof(IEnumerable<Match>), 200)]
    public async Task<ActionResult<IEnumerable<Match>>> GetMatches([FromQuery] GetMatchesRequestDto request)
    {
        try
        {
            var matches = await _matchService.GetMatchesAsync(request);
            return Ok(matches);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
