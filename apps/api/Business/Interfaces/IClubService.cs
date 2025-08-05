using Api.DTOs;

namespace Api.Business.Interfaces;

public interface IClubService
{
    Task<List<ClubDto>> GetAllClubsAsync();
    Task<ClubDto> FindOrCreateClubAsync(FindOrCreateClubRequestDto request);
    Task<int?> FindClubIdAsync(string externalId);
}
