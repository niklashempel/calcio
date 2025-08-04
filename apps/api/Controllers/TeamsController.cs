using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Api.Data;
using Api.Models;
using Api.DTOs;
using Api.Extensions;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Tags("Teams")]
public class TeamsController : ControllerBase
{
    private readonly CalcioDbContext _context;

    public TeamsController(CalcioDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Find or create team
    /// </summary>
    /// <param name="request">Team creation/find request</param>
    /// <returns>The found or created team</returns>
    [HttpPost("find-or-create")]
    [ProducesResponseType(typeof(TeamDto), 200)]
    [ProducesResponseType(typeof(TeamDto), 201)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<TeamDto>> FindOrCreateTeam([FromBody] FindOrCreateTeamRequest request)
    {
        // First try to find by external ID if provided
        if (!string.IsNullOrEmpty(request.ExternalId))
        {
            var existingTeam = await _context.Teams.Include(t => t.Club)
                .FirstOrDefaultAsync(t => t.ExternalId == request.ExternalId);
            if (existingTeam != null)
            {
                existingTeam.Name = request.Name;
                await _context.SaveChangesAsync();
                return Ok(existingTeam.ToDto());
            }
        }

        // Find the club first
        var club = await _context.Clubs.FirstOrDefaultAsync(c => c.ExternalId == request.ClubExternalId);
        if (club == null)
        {
            return BadRequest($"Club with external ID {request.ClubExternalId} not found");
        }

        // Try to find existing team by name and club
        var existingTeamByName = await _context.Teams.Include(t => t.Club)
            .FirstOrDefaultAsync(t => t.Name == request.Name && t.ClubId == club.Id);
        if (existingTeamByName != null)
        {
            existingTeamByName.ExternalId = request.ExternalId; // Update external ID if provided
            await _context.SaveChangesAsync();
            return Ok(existingTeamByName.ToDto());
        }

        // Create new team
        var newTeam = new Team
        {
            Name = request.Name,
            ClubId = club.Id,
            ExternalId = request.ExternalId
        };
        _context.Teams.Add(newTeam);
        await _context.SaveChangesAsync();

        // Load the team with club for DTO
        var createdTeam = await _context.Teams.Include(t => t.Club)
            .FirstAsync(t => t.Id == newTeam.Id);

        return CreatedAtAction(nameof(FindOrCreateTeam), new { id = newTeam.Id }, createdTeam.ToDto());
    }
}
