using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NetTopologySuite.Geometries;

namespace Api.Models;

[Table("venues")]
public class Venue
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("address")]
    public string? Address { get; set; }

    [Column("location", TypeName = "geometry")]
    public Point? Location { get; set; }

    public virtual ICollection<Match> Matches { get; set; } = new List<Match>();
}
