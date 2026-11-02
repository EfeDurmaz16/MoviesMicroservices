using CORE.Domain;

namespace Movies.APP.Domain;

public class MovieGenre : Record
{
    public int MovieId { get; set; }

    public Movie Movie { get; set; }

    public int GenreId { get; set; }

    public Genre Genre { get; set; }
}
