using Microsoft.EntityFrameworkCore;
using Api.Business.Interfaces;
using Api.Data;
using Api.DTOs;
using Api.Extensions;
using Api.Models;

namespace Api.Business.Services;

public class CompetitionService : ICompetitionService
{
    private readonly CalcioDbContext _context;

    public CompetitionService(CalcioDbContext context)
    {
        _context = context;
    }

    public async Task<CompetitionDto> FindOrCreateCompetitionAsync(FindOrCreateCompetitionRequestDto request)
    {
        var existing = await _context.Competitions.FirstOrDefaultAsync(c => c.Name == request.Name);
        if (existing != null)
        {
            return existing.ToDto();
        }

        var newCompetition = new Competition
        {
            Name = request.Name
        };
        _context.Competitions.Add(newCompetition);
        await _context.SaveChangesAsync();
        return newCompetition.ToDto();
    }

    public async Task<int?> FindCompetitionIdAsync(string name)
    {
        var competition = await _context.Competitions.FirstOrDefaultAsync(c => c.Name == name);
        return competition?.Id;
    }
}
