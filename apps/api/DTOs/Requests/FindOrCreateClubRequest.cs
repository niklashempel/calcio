namespace Api.DTOs;

public class FindOrCreateClubRequestDto
{
    public string ExternalId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string PostCode { get; set; } = string.Empty;
}
