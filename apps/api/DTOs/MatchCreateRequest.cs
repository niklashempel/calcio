namespace Api.DTOs;

public class MatchCreateRequest
{
    public string Url { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;
    public int HomeTeamId { get; set; }
    public int AwayTeamId { get; set; }
    public int VenueId { get; set; }
    public int AgeGroupId { get; set; }
    public int CompetitionId { get; set; }
}
