using Api.Models;
using Api.DTOs;

namespace Api.Extensions;

public static class EntityExtensions
{
    public static ClubDto ToDto(this Club club)
    {
        return new ClubDto
        {
            Id = club.Id,
            ExternalId = club.ExternalId,
            Name = club.Name,
            Teams = club.Teams?.Select(t => new TeamDto
            {
                Id = t.Id,
                Name = t.Name,
                ClubId = t.ClubId
            }).ToList()
        };
    }

    public static TeamDto ToDto(this Team team)
    {
        return new TeamDto
        {
            Id = team.Id,
            Name = team.Name,
            ClubId = team.ClubId,
            ClubName = team.Club?.Name
        };
    }

    public static VenueDto ToDto(this Venue venue)
    {
        return new VenueDto
        {
            Id = venue.Id,
            Address = venue.Address,
            Latitude = venue.Location?.Y,
            Longitude = venue.Location?.X
        };
    }

    public static AgeGroupDto ToDto(this AgeGroup ageGroup)
    {
        return new AgeGroupDto
        {
            Id = ageGroup.Id,
            Name = ageGroup.Name
        };
    }

    public static CompetitionDto ToDto(this Competition competition)
    {
        return new CompetitionDto
        {
            Id = competition.Id,
            Name = competition.Name
        };
    }

    public static MatchDto ToDto(this Match match)
    {
        return new MatchDto
        {
            Id = match.Id,
            Url = match.Url,
            Time = match.Time,
            HomeTeam = match.HomeTeam?.ToDto(),
            AwayTeam = match.AwayTeam?.ToDto(),
            Venue = match.Venue?.ToDto(),
            AgeGroup = match.AgeGroup?.ToDto(),
            Competition = match.Competition == null ? null : new CompetitionDto
            {
                Id = match.Competition.Id,
                Name = match.Competition.Name
            }
        };
    }
}
