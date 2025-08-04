namespace Api.DTOs;

public class FindOrCreateTeamRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string ClubExternalId { get; set; } = string.Empty;
    public string? ExternalId { get; set; }
}
