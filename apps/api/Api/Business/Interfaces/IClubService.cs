using Calcio.Api.Core.DTOs;
using Api.DTOs.Requests;

namespace Api.Business.Interfaces;

public interface IClubService
{
    Task<List<ClubDto>> GetClubsAsync(GetClubsRequestDto request);
    Task<ClubDto> FindOrCreateClubAsync(FindOrCreateClubRequestDto request);
    Task<int?> FindClubIdAsync(string externalId);
}
