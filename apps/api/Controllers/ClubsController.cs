using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Api.Data;
using Api.Models;
using Api.DTOs;
using Api.Extensions;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Tags("Clubs")]
public class ClubsController : ControllerBase
{
    private readonly CalcioDbContext _context;

    public ClubsController(CalcioDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Get all clubs
    /// </summary>
    /// <returns>List of all football clubs with their associated teams</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<ClubDto>), 200)]
    public async Task<ActionResult<List<ClubDto>>> GetClubs()
    {
        var clubs = await _context.Clubs.Include(c => c.Teams).ToListAsync();
        return Ok(clubs.Select(c => c.ToDto()).ToList());
    }

    /// <summary>
    /// Find or create club by external ID
    /// </summary>
    /// <param name="request">Club creation/find request</param>
    /// <returns>The found or created club</returns>
    [HttpPost("find-or-create")]
    [ProducesResponseType(typeof(ClubDto), 200)]
    [ProducesResponseType(typeof(ClubDto), 201)]
    public async Task<ActionResult<ClubDto>> FindOrCreateClub([FromBody] FindOrCreateClubRequest request)
    {
        var existingClub = await _context.Clubs.FirstOrDefaultAsync(c => c.ExternalId == request.ExternalId);
        if (existingClub != null)
        {
            existingClub.Name = request.Name;
            await _context.SaveChangesAsync();
            return Ok(existingClub.ToDto());
        }

        var newClub = new Club { ExternalId = request.ExternalId, Name = request.Name };
        _context.Clubs.Add(newClub);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetClubs), new { id = newClub.Id }, newClub.ToDto());
    }
}
