using CORE.Domain;
namespace Movies.APP.Domain;

public class Movie : Record
{
    public string Name { get; set; }
    public DateTime? ReleaseDate { get; set; }
    public decimal TotalRevenue { get; set; }
    public int DirectorId { get; set; }
}
