using Microsoft.AspNetCore.Mvc;
using Api.DTOs;
using Api.Models;
using Api.Business.Interfaces;

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
    /// Create a new match
    /// </summary>
    /// <param name="request">Match creation request</param>
    /// <returns>The created match</returns>
    [HttpPost]
    [ProducesResponseType(typeof(Match), 201)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<Match>> CreateMatch([FromBody] CreateMatchRequestDto request)
    {
        try
        {
            var match = await _matchService.CreateMatchAsync(request);
            return CreatedAtAction(nameof(CreateMatch), new { id = match.Id }, match);
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
}
