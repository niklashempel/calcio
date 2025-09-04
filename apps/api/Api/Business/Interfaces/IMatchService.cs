using Calcio.Api.Core.DTOs;
using Api.DTOs.Requests;
using Api.Models;

namespace Api.Business.Interfaces;

public interface IMatchService
{
    Task<Match> UpsertMatchAsync(UpsertMatchRequestDto request);

    Task<IEnumerable<GroupedMatchesByVenueDto>> GetMatchesAsync(GetMatchesRequestDto request);

    Task<MatchFilterOptionsDto> GetMatchFilterOptionsAsync();
}