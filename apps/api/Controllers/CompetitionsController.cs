using Microsoft.AspNetCore.Mvc;
using Api.DTOs;
using Api.Business.Interfaces;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Tags("Competitions")]
public class CompetitionsController : ControllerBase
{
    private readonly ICompetitionService _competitionService;

    public CompetitionsController(ICompetitionService competitionService)
    {
        _competitionService = competitionService;
    }

    /// <summary>
    /// Find or create competition by name
    /// </summary>
    /// <param name="request">Competition creation/find request</param>
    /// <returns>The found or created competition</returns>
    [HttpPost("find-or-create")]
    [ProducesResponseType(typeof(CompetitionDto), 200)]
    [ProducesResponseType(typeof(CompetitionDto), 201)]
    public async Task<ActionResult<CompetitionDto>> FindOrCreateCompetition([FromBody] FindOrCreateCompetitionRequestDto request)
    {
        var competition = await _competitionService.FindOrCreateCompetitionAsync(request);
        return Ok(competition);
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
        var competitionId = await _competitionService.FindCompetitionIdAsync(name);
        return competitionId.HasValue ? Ok(competitionId.Value) : NotFound();
    }
}
