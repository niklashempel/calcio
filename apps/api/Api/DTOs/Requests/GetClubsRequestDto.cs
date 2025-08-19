namespace Api.DTOs.Requests;

public class GetClubsRequestDto
{
    public IEnumerable<string>? PostCodes { get; set; }
}