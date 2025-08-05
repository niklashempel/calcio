using Api.DTOs;
using Api.Models;

namespace Api.Business.Interfaces;

public interface IMatchService
{
    Task<Match> CreateMatchAsync(CreateMatchRequestDto request);
}
