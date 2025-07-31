namespace Api.DTOs;

public class CreateVenueRequest
{
    public string Address { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}
