using Calcio.Api.Core.DTOs;
using Calcio.Api.Core.Logic;

namespace Api.Core.Tests.Logic;

public class MatchGroupingTests
{
    private static MatchDto MakeMatch(int id, int venueId, DateTime? timeUtc) => new()
    {
        Id = id,
        Time = timeUtc.HasValue ? DateTime.SpecifyKind(timeUtc.Value, DateTimeKind.Utc) : null,
        Venue = new VenueDto { Id = venueId, Latitude = 52, Longitude = 13 }
    };

    [Fact]
    public void GroupByTime_WithNoMatches_ReturnsEmpty()
    {
        var result = MatchGrouping.GroupByTime(Array.Empty<MatchDto>(), DateTime.UtcNow);
        Assert.Empty(result.Past);
        Assert.Empty(result.Today);
        Assert.Empty(result.Upcoming);
    }

    [Fact]
    public void GroupByTime_ClassifiesMatchesIntoTodayUpcomingPast()
    {
        // Arrange
        var refTime = new DateTime(2025, 8, 30, 12, 0, 0, DateTimeKind.Utc);
        var todayStart = new DateTime(refTime.Year, refTime.Month, refTime.Day, 0, 0, 0, DateTimeKind.Utc);
        var todayEnd = todayStart.AddDays(1);

        var matches = new[]
        {
            MakeMatch(1, 1, todayStart.AddMinutes(1)), // today
            MakeMatch(2, 1, todayEnd.AddMinutes(5)),   // upcoming
            MakeMatch(3, 1, todayStart.AddMinutes(-10)), // past
            MakeMatch(4, 1, null) // null time -> past
        };

        // Act
        var grouped = MatchGrouping.GroupByTime(matches, refTime);

        // Assert
        Assert.Single(grouped.Today);
        Assert.Single(grouped.Upcoming);
        Assert.Equal(2, grouped.Past.Count);
    }

    [Fact]
    public void GroupByTime_SortsMatchesAscendingWithinBuckets()
    {
        // Arrange
        var refTime = new DateTime(2025, 8, 30, 12, 0, 0, DateTimeKind.Utc);
        var todayStart = new DateTime(refTime.Year, refTime.Month, refTime.Day, 0, 0, 0, DateTimeKind.Utc);
        var todayEnd = todayStart.AddDays(1);

        var matches = new[]
        {
            MakeMatch(1, 1, todayStart.AddHours(5)),
            MakeMatch(2, 1, todayStart.AddHours(1)),
            MakeMatch(3, 1, todayEnd.AddHours(2)),
            MakeMatch(4, 1, todayEnd.AddHours(1)),
            MakeMatch(5, 1, todayStart.AddHours(-3)),
            MakeMatch(6, 1, todayStart.AddHours(-4)),
        };

        // Act
        var grouped = MatchGrouping.GroupByTime(matches, refTime);

        // Assert
        Assert.Equal([2, 1], grouped.Today.Select(m => m.Id));
        Assert.Equal([4, 3], grouped.Upcoming.Select(m => m.Id));
        Assert.Equal([6, 5], grouped.Past.Select(m => m.Id));
    }
}
