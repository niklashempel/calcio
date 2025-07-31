namespace Api.DTOs;

public class ClubDto
{
    public int Id { get; set; }
    public string? ExternalId { get; set; }
    public string? Name { get; set; }
    public List<TeamDto>? Teams { get; set; }
}
