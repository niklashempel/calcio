using Microsoft.AspNetCore.Mvc;
using Api.DTOs;
using Api.Business.Interfaces;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Tags("Teams")]
public class TeamsController : ControllerBase
{
    private readonly ITeamService _teamService;

    public TeamsController(ITeamService teamService)
    {
        _teamService = teamService;
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
    public async Task<ActionResult<TeamDto>> FindOrCreateTeam([FromBody] FindOrCreateTeamRequestDto request)
    {
        try
        {
            var team = await _teamService.FindOrCreateTeamAsync(request);
            return Ok(team);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Find team by external ID and return only the ID
    /// </summary>
    /// <param name="externalId">External ID of the team</param>
    /// <returns>Team ID</returns>
    [HttpGet("find/{externalId}/id")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<int>> FindTeamId(string externalId)
    {
        var teamId = await _teamService.FindTeamIdAsync(externalId);
        return teamId.HasValue ? Ok(teamId.Value) : NotFound();
    }
}
