namespace Api.DTOs.Requests;

public class CreateVenueRequestDto
{
    public string Address { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}
