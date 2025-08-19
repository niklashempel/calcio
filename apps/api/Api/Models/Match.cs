using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Models;

[Table("matches")]
public class Match
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("url")]
    public string? Url { get; set; }

    [Column("time")]
    public DateTime? Time { get; set; }

    [Column("home_team_id")]
    public int? HomeTeamId { get; set; }

    [Column("away_team_id")]
    public int? AwayTeamId { get; set; }

    [Column("venue_id")]
    public int? VenueId { get; set; }

    [Column("age_group_id")]
    public int? AgeGroupId { get; set; }

    [Column("competition_id")]
    public int? CompetitionId { get; set; }

    [ForeignKey("HomeTeamId")]
    public virtual Team? HomeTeam { get; set; }

    [ForeignKey("AwayTeamId")]
    public virtual Team? AwayTeam { get; set; }

    [ForeignKey("VenueId")]
    public virtual Venue? Venue { get; set; }

    [ForeignKey("AgeGroupId")]
    public virtual AgeGroup? AgeGroup { get; set; }

    [ForeignKey("CompetitionId")]
    public virtual Competition? Competition { get; set; }
}
