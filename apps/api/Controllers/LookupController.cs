using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Api.Data;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Tags("Lookup")]
public class LookupController : ControllerBase
{
    private readonly CalcioDbContext _context;

    public LookupController(CalcioDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Get club ID by external ID
    /// </summary>
    /// <param name="externalId">External ID of the club</param>
    /// <returns>Club ID</returns>
    [HttpGet("clubs/{externalId}/id")]
    [ProducesResponseType(typeof(int), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<int>> GetClubIdByExternalId(string externalId)
    {
        var club = await _context.Clubs.FirstOrDefaultAsync(c => c.ExternalId == externalId);
        return club is not null ? Ok(club.Id) : NotFound();
    }

    /// <summary>
    /// Get age group ID by name
    /// </summary>
    /// <param name="name">Name of the age group</param>
    /// <returns>Age group ID</returns>
    [HttpGet("age-groups/{name}/id")]
    [ProducesResponseType(typeof(int), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<int>> GetAgeGroupIdByName(string name)
    {
        var ageGroup = await _context.AgeGroups.FirstOrDefaultAsync(ag => ag.Name == name);
        return ageGroup is not null ? Ok(ageGroup.Id) : NotFound();
    }

    /// <summary>
    /// Get competition ID by name
    /// </summary>
    /// <param name="name">Name of the competition</param>
    /// <returns>Competition ID</returns>
    [HttpGet("competitions/{name}/id")]
    [ProducesResponseType(typeof(int), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<int>> GetCompetitionIdByName(string name)
    {
        var competition = await _context.Competitions.FirstOrDefaultAsync(c => c.Name == name);
        return competition is not null ? Ok(competition.Id) : NotFound();
    }

    /// <summary>
    /// Get venue ID by address
    /// </summary>
    /// <param name="address">Address of the venue</param>
    /// <returns>Venue ID</returns>
    [HttpGet("venues/{address}/id")]
    [ProducesResponseType(typeof(int), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<int>> GetVenueIdByAddress(string address)
    {
        var venue = await _context.Venues.FirstOrDefaultAsync(v => v.Address == address);
        return venue is not null ? Ok(venue.Id) : NotFound();
    }
}
