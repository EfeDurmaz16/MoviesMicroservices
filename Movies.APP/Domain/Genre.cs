using CORE.Domain;
using System.ComponentModel.DataAnnotations;

namespace Movies.APP.Domain;

public class Genre : Record
{
    [Required(ErrorMessage = "{0} is required!")]
    public string Name { get; set; }

    public List<MovieGenre> MovieGenres { get; set; } = new();
}
