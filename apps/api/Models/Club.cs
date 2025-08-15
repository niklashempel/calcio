using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Models;

[Table("clubs")]
public class Club
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("external_id")]
    public string? ExternalId { get; set; }

    [Column("name")]
    public string? Name { get; set; }

    public virtual ICollection<Team> Teams { get; set; } = new List<Team>();

    [Column("post_code")]
    public string? PostCode { get; set; }
}
