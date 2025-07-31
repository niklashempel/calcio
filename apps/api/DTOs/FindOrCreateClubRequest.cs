namespace Api.DTOs;

public class FindOrCreateClubRequest
{
    public string ExternalId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
