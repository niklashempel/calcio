using Api.DTOs;

namespace Api.Business.Interfaces;

public interface IVenueService
{
    Task<VenueDto> FindOrCreateVenueAsync(CreateVenueRequestDto request);
    Task<int?> FindVenueIdAsync(string address);
}
