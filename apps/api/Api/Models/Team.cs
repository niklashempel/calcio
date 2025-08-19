using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Models;

[Table("teams")]
public class Team
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("external_id")]
    public string? ExternalId { get; set; }

    [Column("name")]
    public string? Name { get; set; }

    [Column("club_id")]
    public int? ClubId { get; set; }

    [ForeignKey("ClubId")]
    public virtual Club? Club { get; set; }

    public virtual ICollection<Match> HomeMatches { get; set; } = new List<Match>();
    public virtual ICollection<Match> AwayMatches { get; set; } = new List<Match>();
}
