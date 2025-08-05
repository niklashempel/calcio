using Microsoft.AspNetCore.Mvc;
using Api.DTOs;
using Api.Business.Interfaces;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Tags("Clubs")]
public class ClubsController : ControllerBase
{
    private readonly IClubService _clubService;

    public ClubsController(IClubService clubService)
    {
        _clubService = clubService;
    }

    /// <summary>
    /// Get all clubs
    /// </summary>
    /// <returns>List of all football clubs with their associated teams</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<ClubDto>), 200)]
    public async Task<ActionResult<List<ClubDto>>> GetClubs()
    {
        var clubs = await _clubService.GetAllClubsAsync();
        return Ok(clubs);
    }

    /// <summary>
    /// Find or create club by external ID
    /// </summary>
    /// <param name="request">Club creation/find request</param>
    /// <returns>The found or created club</returns>
    [HttpPost("find-or-create")]
    [ProducesResponseType(typeof(ClubDto), 200)]
    [ProducesResponseType(typeof(ClubDto), 201)]
    public async Task<ActionResult<ClubDto>> FindOrCreateClub([FromBody] FindOrCreateClubRequestDto request)
    {
        var club = await _clubService.FindOrCreateClubAsync(request);
        return Ok(club);
    }

    /// <summary>
    /// Find club by external ID and return only the ID
    /// </summary>
    /// <param name="externalId">External ID of the club</param>
    /// <returns>Club ID</returns>
    [HttpGet("find/{externalId}/id")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<int>> FindClubId(string externalId)
    {
        var clubId = await _clubService.FindClubIdAsync(externalId);
        return clubId.HasValue ? Ok(clubId.Value) : NotFound();
    }
}
