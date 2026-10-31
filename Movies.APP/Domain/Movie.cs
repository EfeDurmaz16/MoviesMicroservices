using CORE.Domain;
using System.ComponentModel.DataAnnotations;

namespace Movies.APP.Domain;

public class Movie : Record
{
    [Required(ErrorMessage = "{0} is required!")]
    public string Name { get; set; }
    public DateTime? ReleaseDate { get; set; }
    public decimal TotalRevenue { get; set; }
    public int DirectorId { get; set; }
}
