using Api.DTOs;
using Api.DTOs.Requests;
using Api.Models;

namespace Api.Business.Interfaces;

public interface IMatchService
{
    Task<Match> CreateMatchAsync(CreateMatchRequestDto request);

    Task<IEnumerable<MatchDto>> GetMatchesAsync(GetMatchesRequestDto request);
}
