using Api.DTOs;

namespace Api.Business.Interfaces;

public interface ITeamService
{
    Task<TeamDto> FindOrCreateTeamAsync(FindOrCreateTeamRequestDto request);
    Task<int?> FindTeamIdAsync(string externalId);
}
