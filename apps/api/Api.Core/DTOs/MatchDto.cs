namespace Calcio.Api.Core.DTOs;

public class MatchDto
{
    public int Id { get; set; }
    public string? Url { get; set; }
    public DateTime? Time { get; set; }
    public TeamDto? HomeTeam { get; set; }
    public TeamDto? AwayTeam { get; set; }
    public VenueDto? Venue { get; set; }
    public AgeGroupDto? AgeGroup { get; set; }
    public CompetitionDto? Competition { get; set; }
}
