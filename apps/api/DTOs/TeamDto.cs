namespace Api.DTOs;

public class TeamDto
{
    public int Id { get; set; }
    public string? ExternalId { get; set; }
    public string? Name { get; set; }
    public int? ClubId { get; set; }
    public string? ClubName { get; set; }
}
