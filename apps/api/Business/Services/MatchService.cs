using Microsoft.EntityFrameworkCore;
using Api.Business.Interfaces;
using Api.Data;
using Api.DTOs;
using Api.Models;
using Api.DTOs.Requests;
using Api.Extensions;

namespace Api.Business.Services;

public class MatchService : IMatchService
{
    private readonly CalcioDbContext _context;

    public MatchService(CalcioDbContext context)
    {
        _context = context;
    }

    public async Task<Match> UpsertMatchAsync(UpsertMatchRequestDto request)
    {
        try
        {
            var matchTime = ParseMatchTime(request);

            var url = request.Url;
            var match = await _context.Matches.FirstOrDefaultAsync(m => m.Url == url);
            if (match == null)
            {
                match = new Match
                {
                    Url = url,
                    Time = matchTime,
                    HomeTeamId = request.HomeTeamId,
                    AwayTeamId = request.AwayTeamId,
                    VenueId = request.VenueId,
                    AgeGroupId = request.AgeGroupId,
                    CompetitionId = request.CompetitionId
                };
                _context.Matches.Add(match);
            }
            else
            {
                match.Time = matchTime;
                match.HomeTeamId = request.HomeTeamId;
                match.AwayTeamId = request.AwayTeamId;
                match.VenueId = request.VenueId;
                match.AgeGroupId = request.AgeGroupId;
                match.CompetitionId = request.CompetitionId;
                _context.Matches.Update(match);
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException dbEx)
            {
                // Handle rare race condition where another insert happened with same URL between lookup and save
                if (!string.IsNullOrWhiteSpace(url))
                {
                    var existing = await _context.Matches.AsNoTracking().FirstOrDefaultAsync(m => m.Url == url);
                    if (existing != null)
                    {
                        return existing;
                    }
                }
                throw new InvalidOperationException($"Database error during match upsert: {dbEx.Message}", dbEx);
            }

            return match;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error creating match: {ex.Message}", ex);
        }
    }

    private static DateTime ParseMatchTime(UpsertMatchRequestDto request)
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

        return matchTime;
    }

    public async Task<IEnumerable<MatchDto>> GetMatchesAsync(GetMatchesRequestDto request)
    {
        var query = _context.Matches
            .Include(m => m.Venue)
            .Include(m => m.HomeTeam)
                .ThenInclude(t => t!.Club)
            .Include(m => m.AwayTeam)
                .ThenInclude(t => t!.Club)
            .Include(m => m.Competition)
            .Include(m => m.AgeGroup)
            .AsQueryable();

        if (request.HasValidBoundingBox)
        {
            query = query.Where(m => m.Venue != null && m.Venue.Location != null &&
                m.Venue.Location.Y >= request.MinLat && m.Venue.Location.Y <= request.MaxLat &&
                m.Venue.Location.X >= request.MinLng && m.Venue.Location.X <= request.MaxLng);
        }

        return await query.Select(x => x.ToDto()).ToListAsync();
    }
}
