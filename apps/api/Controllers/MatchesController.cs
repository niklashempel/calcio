using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Api.Data;
using Api.Models;
using Api.DTOs;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Tags("Matches")]
public class MatchesController : ControllerBase
{
    private readonly CalcioDbContext _context;

    public MatchesController(CalcioDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Create a new match
    /// </summary>
    /// <param name="request">Match creation request</param>
    /// <returns>The created match</returns>
    [HttpPost]
    [ProducesResponseType(typeof(Match), 201)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<Match>> CreateMatch([FromBody] MatchCreateRequest request)
    {
        try
        {
            // Parse the time string and ensure it's in UTC
            DateTime matchTime;
            if (DateTime.TryParse(request.Time, out matchTime))
            {
                // If the DateTime doesn't have a timezone specified, assume it's UTC
                if (matchTime.Kind == DateTimeKind.Unspecified)
                {
                    matchTime = DateTime.SpecifyKind(matchTime, DateTimeKind.Utc);
                }
                // If it's local time, convert to UTC
                else if (matchTime.Kind == DateTimeKind.Local)
                {
                    matchTime = matchTime.ToUniversalTime();
                }
            }
            else
            {
                return BadRequest("Invalid time format");
            }

            var match = new Match
            {
                Url = request.Url,
                Time = matchTime, // Now guaranteed to be UTC
                HomeTeamId = request.HomeTeamId,
                AwayTeamId = request.AwayTeamId,
                VenueId = request.VenueId,
                AgeGroupId = request.AgeGroupId,
                CompetitionId = request.CompetitionId
            };

            _context.Matches.Add(match);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(CreateMatch), new { id = match.Id }, match);
        }
        catch (Exception ex)
        {
            return BadRequest($"Error creating match: {ex.Message}");
        }
    }
}
