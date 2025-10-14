using System.Text.RegularExpressions;
using Calcio.Api.Core.DTOs;

namespace Calcio.Api.Core.Logic;

public static class MatchGrouping
{
    public static GroupedMatchesByVenueDto GroupByTime(IEnumerable<MatchDto> matches, DateTime referenceUtc)
    {
        var list = matches.Where(m => m.Venue?.Id != null && m.Venue.Latitude.HasValue && m.Venue.Longitude.HasValue);

        var todayStart = new DateTime(referenceUtc.Year, referenceUtc.Month, referenceUtc.Day, 0, 0, 0, DateTimeKind.Utc);
        var todayEnd = todayStart.AddDays(1);

        var groupedMatch = new GroupedMatchesByVenueDto();

        foreach (var match in list)
        {
            var venueId = match.Venue!.Id;

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
        }

        groupedMatch.Today = groupedMatch.Today.OrderBy(m => m.Time).ToList();
        groupedMatch.Upcoming = groupedMatch.Upcoming.OrderBy(m => m.Time).ToList();
        groupedMatch.Past = groupedMatch.Past.OrderBy(m => m.Time).ToList();

        return groupedMatch;
    }
}
