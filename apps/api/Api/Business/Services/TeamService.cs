using Microsoft.EntityFrameworkCore;
using Api.Business.Interfaces;
using Api.Data;
using Calcio.Api.Core.DTOs;
using Api.DTOs.Requests;
using Api.Extensions;
using Api.Models;

namespace Api.Business.Services;

public class TeamService : ITeamService
{
    private readonly CalcioDbContext _context;

    public TeamService(CalcioDbContext context)
    {
        _context = context;
    }

    public async Task<TeamDto> FindOrCreateTeamAsync(FindOrCreateTeamRequestDto request)
    {
        // First try to find by external ID if provided
        if (!string.IsNullOrEmpty(request.ExternalId))
        {
            var existingTeam = await _context.Teams.Include(t => t.Club)
                .FirstOrDefaultAsync(t => t.ExternalId == request.ExternalId);
            if (existingTeam != null)
            {
                existingTeam.Name = request.Name;
                await _context.SaveChangesAsync();
                return existingTeam.ToDto();
            }
        }

        var club = await _context.Clubs.FirstOrDefaultAsync(c => c.ExternalId == request.ClubExternalId) ?? throw new ArgumentException($"Club with external ID {request.ClubExternalId} not found");

        var newTeam = new Team
        {
            Name = request.Name,
            ClubId = club.Id,
            ExternalId = request.ExternalId
        };
        _context.Teams.Add(newTeam);
        await _context.SaveChangesAsync();

        var createdTeam = await _context.Teams.Include(t => t.Club)
            .FirstAsync(t => t.Id == newTeam.Id);

        return createdTeam.ToDto();
    }

    public async Task<int?> FindTeamIdAsync(string externalId)
    {
        var team = await _context.Teams.FirstOrDefaultAsync(t => t.ExternalId == externalId);
        return team?.Id;
    }
}
