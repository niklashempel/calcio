using Microsoft.EntityFrameworkCore;
using Api.Business.Interfaces;
using Api.Data;
using Api.DTOs;
using Api.Extensions;
using Api.Models;

namespace Api.Business.Services;

public class AgeGroupService : IAgeGroupService
{
    private readonly CalcioDbContext _context;

    public AgeGroupService(CalcioDbContext context)
    {
        _context = context;
    }

    public async Task<AgeGroupDto> FindOrCreateAgeGroupAsync(FindOrCreateAgeGroupRequestDto request)
    {
        var existing = await _context.AgeGroups.FirstOrDefaultAsync(ag => ag.Name == request.Name);
        if (existing != null)
        {
            return existing.ToDto();
        }

        var newAgeGroup = new AgeGroup { Name = request.Name };
        _context.AgeGroups.Add(newAgeGroup);
        await _context.SaveChangesAsync();
        return newAgeGroup.ToDto();
    }

    public async Task<int?> FindAgeGroupIdAsync(string name)
    {
        var ageGroup = await _context.AgeGroups.FirstOrDefaultAsync(ag => ag.Name == name);
        return ageGroup?.Id;
    }
}
