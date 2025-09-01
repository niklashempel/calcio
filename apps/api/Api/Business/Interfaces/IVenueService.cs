using Calcio.Api.Core.DTOs;
using Api.DTOs.Requests;

namespace Api.Business.Interfaces;

public interface IVenueService
{
    Task<VenueDto> FindOrCreateVenueAsync(CreateVenueRequestDto request);
    Task<int?> FindVenueIdAsync(string address);

    Task<VenueDto?> UpdateVenueAsync(int id, UpdateVenueDto request);
}
