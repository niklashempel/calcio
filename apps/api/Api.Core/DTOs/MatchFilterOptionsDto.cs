namespace Calcio.Api.Core.DTOs;

public class MatchFilterOptionsDto
{
    public DateTime? MinDate { get; set; }

    public DateTime? MaxDate { get; set; }

    public IEnumerable<CompetitionFilterDto> Competitions { get; set; } = [];

    public IEnumerable<AgeGroupFilterDto> AgeGroups { get; set; } = [];
}
