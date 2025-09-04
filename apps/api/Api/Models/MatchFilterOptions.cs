namespace Api.Models;

public class MatchFilterOptions
{
    public DateTime? MinDate { get; set; }

    public DateTime? MaxDate { get; set; }

    public IEnumerable<Competition> Competitions { get; set; } = [];

    public IEnumerable<AgeGroup> AgeGroups { get; set; } = [];
}