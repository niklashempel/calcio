using Microsoft.EntityFrameworkCore;
using Api.Business.Interfaces;
using Api.Data;
using Api.DTOs;
using Api.Extensions;
using Api.Models;

namespace Api.Business.Services;

public class ClubService : IClubService
{
    private readonly CalcioDbContext _context;

    public ClubService(CalcioDbContext context)
    {
        _context = context;
    }

    public async Task<List<ClubDto>> GetAllClubsAsync()
    {
        var clubs = await _context.Clubs.Include(c => c.Teams).ToListAsync();
        return clubs.Select(c => c.ToDto()).ToList();
    }

    public async Task<ClubDto> FindOrCreateClubAsync(FindOrCreateClubRequestDto request)
    {
        var existingClub = await _context.Clubs.FirstOrDefaultAsync(c => c.ExternalId == request.ExternalId);
        if (existingClub != null)
        {
            if (request.PostCode != null)
            {
                existingClub.PostCode = request.PostCode;
            }
            await _context.SaveChangesAsync();
            return existingClub.ToDto();
        }

        var newClub = new Club { ExternalId = request.ExternalId, Name = request.Name, PostCode = request.PostCode };
        _context.Clubs.Add(newClub);
        await _context.SaveChangesAsync();
        return newClub.ToDto();
    }

    public async Task<int?> FindClubIdAsync(string externalId)
    {
        var club = await _context.Clubs.FirstOrDefaultAsync(c => c.ExternalId == externalId);
        return club?.Id;
    }
}
