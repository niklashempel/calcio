using Calcio.Api.Core.DTOs;

namespace Calcio.Api.Core.Logic;

public static class MatchGrouping
{
    public static IEnumerable<GroupedMatchesByVenueDto> GroupByVenue(IEnumerable<MatchDto> matches, DateTime referenceUtc)
    {
        var list = matches.Where(m => m.Venue?.Id != null && m.Venue.Latitude.HasValue && m.Venue.Longitude.HasValue);
        var groups = new Dictionary<int, GroupedMatchesByVenueDto>();

        var todayStart = new DateTime(referenceUtc.Year, referenceUtc.Month, referenceUtc.Day, 0, 0, 0, DateTimeKind.Utc);
        var todayEnd = todayStart.AddDays(1);

        foreach (var match in list)
        {
            var venueId = match.Venue!.Id;
            if (!groups.TryGetValue(venueId, out var groupedMatch))
            {
                groupedMatch = new GroupedMatchesByVenueDto
                {
                    VenueId = venueId,
                    Venue = match.Venue
                };
                groups[venueId] = groupedMatch;
            }

            if (match.Time.HasValue)
            {
                var time = match.Time.Value.Kind == DateTimeKind.Utc ? match.Time.Value : match.Time.Value.ToUniversalTime();
                if (time >= todayStart && time < todayEnd) groupedMatch.Today.Add(match);
                else if (time >= todayEnd) groupedMatch.Upcoming.Add(match);
                else groupedMatch.Past.Add(match);
            }
            else
            {
                groupedMatch.Past.Add(match);
            }
            groupedMatch.Count++;
        }

        foreach (var g in groups.Values)
        {
            g.Today = g.Today.OrderBy(m => m.Time).ToList();
            g.Upcoming = g.Upcoming.OrderBy(m => m.Time).ToList();
            g.Past = g.Past.OrderBy(m => m.Time).ToList();
        }

        return groups.Values.OrderBy(v => v.VenueId).ToList();
    }
}
