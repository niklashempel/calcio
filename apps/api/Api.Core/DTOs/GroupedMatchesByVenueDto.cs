namespace Calcio.Api.Core.DTOs;

public class GroupedMatchesByVenueDto
{
    public int VenueId { get; set; }
    public VenueDto? Venue { get; set; }
    public int Count { get; set; }
    public IList<MatchDto> Today { get; set; } = new List<MatchDto>();
    public IList<MatchDto> Upcoming { get; set; } = new List<MatchDto>();
    public IList<MatchDto> Past { get; set; } = new List<MatchDto>();
}
