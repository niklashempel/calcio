using Api.DTOs;

namespace Api.Business.Interfaces;

public interface ICompetitionService
{
    Task<CompetitionDto> FindOrCreateCompetitionAsync(FindOrCreateCompetitionRequestDto request);
    Task<int?> FindCompetitionIdAsync(string name);
}
