using Microsoft.AspNetCore.Mvc;
using Api.DTOs;
using Api.Business.Interfaces;

namespace Api.Controllers;

[ApiController]
[Route("api/age-groups")]
[Tags("Age Groups")]
public class AgeGroupsController : ControllerBase
{
    private readonly IAgeGroupService _ageGroupService;

    public AgeGroupsController(IAgeGroupService ageGroupService)
    {
        _ageGroupService = ageGroupService;
    }

    /// <summary>
    /// Find or create age group by name
    /// </summary>
    /// <param name="request">Age group creation/find request</param>
    /// <returns>The found or created age group</returns>
    [HttpPost("find-or-create")]
    [ProducesResponseType(typeof(AgeGroupDto), 200)]
    public async Task<ActionResult<AgeGroupDto>> FindOrCreateAgeGroup([FromBody] FindOrCreateAgeGroupRequestDto request)
    {
        var ageGroup = await _ageGroupService.FindOrCreateAgeGroupAsync(request);
        return Ok(ageGroup);
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
        var ageGroupId = await _ageGroupService.FindAgeGroupIdAsync(name);
        return ageGroupId.HasValue ? Ok(ageGroupId.Value) : NotFound();
    }
}
