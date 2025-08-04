using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Api.Data;
using Api.Models;
using Api.DTOs;
using Api.Extensions;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Tags("Competitions")]
public class CompetitionsController : ControllerBase
{
    private readonly CalcioDbContext _context;

    public CompetitionsController(CalcioDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Find or create competition by name
    /// </summary>
    /// <param name="request">Competition creation/find request</param>
    /// <returns>The found or created competition</returns>
    [HttpPost("find-or-create")]
    [ProducesResponseType(typeof(FindOrCreateCompetitionRequestDto), 200)]
    [ProducesResponseType(typeof(FindOrCreateCompetitionRequestDto), 201)]
    public async Task<ActionResult<FindOrCreateCompetitionRequestDto>> FindOrCreateCompetition([FromBody] FindOrCreateCompetitionRequestDto request)
    {
        var existing = await _context.Competitions.FirstOrDefaultAsync(c => c.Name == request.Name);
        if (existing != null)
        {
            return Ok(existing.ToDto());
        }

        var newCompetition = new Competition
        {
            Name = request.Name
        };
        _context.Competitions.Add(newCompetition);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(FindOrCreateCompetition), new { id = newCompetition.Id }, newCompetition.ToDto());
    }

    /// <summary>
    /// Find competition by name and return only the ID
    /// </summary>
    /// <param name="name">Name of the competition</param>
    /// <returns>Competition ID</returns>
    [HttpGet("find/{name}/id")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<int>> FindCompetitionId(string name)
    {
        var competition = await _context.Competitions.FirstOrDefaultAsync(c => c.Name == name);
        return competition is not null ? Ok(competition.Id) : NotFound();
    }
}
