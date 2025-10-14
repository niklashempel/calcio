using Microsoft.EntityFrameworkCore;
using Api.Business.Interfaces;
using Api.Data;
using Calcio.Api.Core.DTOs;
using Api.Models;
using Api.DTOs.Requests;
using Api.Extensions;

namespace Api.Business.Services;

public class MatchService : IMatchService
{
    private readonly CalcioDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;

    public MatchService(CalcioDbContext context, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
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
        // Parse the time string; support ISO8601 with or without offset.
        if (!DateTime.TryParse(request.Time, out DateTime matchTime))
        {
            throw new ArgumentException("Invalid time format");
        }

        // If the DateTime has no kind (no offset supplied), interpret it as Europe/Berlin local time.
        if (matchTime.Kind == DateTimeKind.Unspecified)
        {
            try
            {
                // Europe/Berlin daylight saving aware conversion
                var tz = TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");
                matchTime = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(matchTime, DateTimeKind.Unspecified), tz);
            }
            catch (TimeZoneNotFoundException)
            {
                // Fallback: treat as local machine time
                matchTime = DateTime.SpecifyKind(matchTime, DateTimeKind.Local).ToUniversalTime();
            }
            catch (InvalidTimeZoneException)
            {
                matchTime = DateTime.SpecifyKind(matchTime, DateTimeKind.Local).ToUniversalTime();
            }
        }
        else if (matchTime.Kind == DateTimeKind.Local)
        {
            // Convert local to UTC
            matchTime = matchTime.ToUniversalTime();
        }
        // If already UTC, keep as is
        return matchTime;
    }

    public async Task<IEnumerable<MatchLocationDto>> GetMatchLocationsAsync(GetMatchLocationsRequestDto request)
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

        if (request.HasValidDateRange)
        {
            query = query.Where(m => m.Time.HasValue && request.MinDate!.Value.Date <= m.Time.Value.Date && m.Time.Value.Date <= request.MaxDate!.Value.Date);
        }

        if (request.Competitions != null)
        {
            query = query.Where(m => request.Competitions.Contains(m.CompetitionId));
        }

        if (request.AgeGroups != null)
        {
            query = query.Where(m => request.AgeGroups.Contains(m.AgeGroupId));
        }

        var venues = await query.Select(x => x.Venue).Distinct().ToListAsync();
        return venues.Select(x => new MatchLocationDto
        {
            Venue = x?.ToDto()
        });
    }

    public async Task<MatchFilterOptionsDto> GetMatchFilterOptionsAsync()
    {
        var dateRange = await _context.Matches
            .Where(m => m.Time.HasValue)
            .GroupBy(m => 1)
            .Select(g => new
            {
                MinDate = g.Min(m => m.Time),
                MaxDate = g.Max(m => m.Time)
            })
            .FirstOrDefaultAsync();

        var competitions = await _context.Matches
            .Include(m => m.Competition)
            .Where(m => m.Competition != null)
            .Select(m => m.Competition!)
            .Distinct()
            .ToListAsync();

        var ageGroups = await _context.Matches
            .Include(m => m.AgeGroup)
            .Where(m => m.AgeGroup != null)
            .Select(m => m.AgeGroup!)
            .Distinct()
            .ToListAsync();

        var filterOptions = new MatchFilterOptions
        {
            MinDate = dateRange?.MinDate,
            MaxDate = dateRange?.MaxDate,
            Competitions = competitions,
            AgeGroups = ageGroups
        };

        return filterOptions.ToDto();
    }

    public async Task<GroupedMatchesByVenueDto> GetMatchesByVenueAsync(int venueId, GetMatchesByVenueRequestDto request)
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

        query = query.Where(m => m.VenueId == venueId);

        if (request.HasValidDateRange)
        {
            query = query.Where(m => m.Time.HasValue && request.MinDate!.Value.Date <= m.Time.Value.Date && m.Time.Value.Date <= request.MaxDate!.Value.Date);
        }

        if (request.Competitions != null)
        {
            query = query.Where(m => request.Competitions.Contains(m.CompetitionId));
        }

        if (request.AgeGroups != null)
        {
            query = query.Where(m => request.AgeGroups.Contains(m.AgeGroupId));
        }

        var matches = await query.Select(x => x.ToDto()).ToListAsync();

        return Calcio.Api.Core.Logic.MatchGrouping.GroupByTime(matches, _dateTimeProvider.Now);
    }

}
