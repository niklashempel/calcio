using Api.DTOs;

namespace Api.Business.Interfaces;

public interface IAgeGroupService
{
    Task<AgeGroupDto> FindOrCreateAgeGroupAsync(FindOrCreateAgeGroupRequestDto request);
    Task<int?> FindAgeGroupIdAsync(string name);
}
