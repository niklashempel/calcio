using Microsoft.EntityFrameworkCore;
using Api.Business.Interfaces;
using Api.Data;
using Api.DTOs;
using Api.Models;

namespace Api.Business.Services;

public class MatchService : IMatchService
{
    private readonly CalcioDbContext _context;

    public MatchService(CalcioDbContext context)
    {
        _context = context;
    }

    public async Task<Match> CreateMatchAsync(CreateMatchRequestDto request)
    {
        try
        {
            // Parse the time string and ensure it's in UTC
            if (!DateTime.TryParse(request.Time, out DateTime matchTime))
            {
                throw new ArgumentException("Invalid time format");
            }

            // If the DateTime doesn't have a timezone specified, assume it's UTC
            if (matchTime.Kind == DateTimeKind.Unspecified)
            {
                matchTime = DateTime.SpecifyKind(matchTime, DateTimeKind.Utc);
            }
            // If it's local time, convert to UTC
            else if (matchTime.Kind == DateTimeKind.Local)
            {
                matchTime = matchTime.ToUniversalTime();
            }

            var match = new Match
            {
                Url = request.Url,
                Time = matchTime,
                HomeTeamId = request.HomeTeamId,
                AwayTeamId = request.AwayTeamId,
                VenueId = request.VenueId,
                AgeGroupId = request.AgeGroupId,
                CompetitionId = request.CompetitionId
            };

            _context.Matches.Add(match);
            await _context.SaveChangesAsync();
            return match;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error creating match: {ex.Message}", ex);
        }
    }
}
