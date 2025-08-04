using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Api.Data;
using Api.Models;
using Api.DTOs;
using Api.Extensions;

namespace Api.Controllers;

[ApiController]
[Route("api/age-groups")]
[Tags("Age Groups")]
public class AgeGroupsController : ControllerBase
{
    private readonly CalcioDbContext _context;

    public AgeGroupsController(CalcioDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Find or create age group by name
    /// </summary>
    /// <param name="request">Age group creation/find request</param>
    /// <returns>The found or created age group</returns>
    [HttpPost("find-or-create")]
    [ProducesResponseType(typeof(AgeGroupDto), 200)]
    [ProducesResponseType(typeof(AgeGroupDto), 201)]
    public async Task<ActionResult<AgeGroupDto>> FindOrCreateAgeGroup([FromBody] FindOrCreateAgeGroupRequestDto request)
    {
        var existing = await _context.AgeGroups.FirstOrDefaultAsync(ag => ag.Name == request.Name);
        if (existing != null)
        {
            return Ok(existing.ToDto());
        }

        var newAgeGroup = new AgeGroup { Name = request.Name };
        _context.AgeGroups.Add(newAgeGroup);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(FindOrCreateAgeGroup), new { id = newAgeGroup.Id }, newAgeGroup.ToDto());
    }

    /// <summary>
    /// Find age group by name and return only the ID
    /// </summary>
    /// <param name="name">Name of the age group</param>
    /// <returns>Age group ID</returns>
    [HttpGet("find/{name}/id")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<int>> FindAgeGroupId(string name)
    {
        var ageGroup = await _context.AgeGroups.FirstOrDefaultAsync(ag => ag.Name == name);
        return ageGroup is not null ? Ok(ageGroup.Id) : NotFound();
    }
}
