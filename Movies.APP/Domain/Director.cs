using CORE.Domain;
using System.ComponentModel.DataAnnotations;

namespace Movies.APP.Domain;

public class Director : Record
{
    [Required, StringLength(50)]
    public string FirstName { get; set; }

    [Required, StringLength(50)]
    public string LastName { get; set; }

    public bool IsRetired { get; set; }

    public List<Movie> Movies { get; set; } = new();
}
