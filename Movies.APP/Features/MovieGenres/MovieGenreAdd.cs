using CORE.Domain;
using CORE.Models;
using CORE.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using Movies.APP.Domain;

namespace Movies.APP.Features.MovieGenres
{
    public class MovieGenreAddRequest : Record, IRequest<CommandResponse>
    {
        [Required(ErrorMessage = "{0} is required!")]
        [Range(1, int.MaxValue, ErrorMessage = "{0} must be positive!")]
        public int? MovieId { get; set; }

        [Required(ErrorMessage = "{0} is required!")]
        [Range(1, int.MaxValue, ErrorMessage = "{0} must be positive!")]
        public int? GenreId { get; set; }
    }

    public class MovieGenreAddHandler : DbService<MovieGenre>,
        IRequestHandler<MovieGenreAddRequest, CommandResponse>
    {
        private readonly DbContext _db;

        public MovieGenreAddHandler(DbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<CommandResponse> Handle(
            MovieGenreAddRequest request, CancellationToken cancellationToken)
        {
            var movieId = request.MovieId.Value;
            var genreId = request.GenreId.Value;

            if (!await _db.Set<Movie>().AnyAsync(
                movie => movie.Id == movieId, cancellationToken))
                return Error("Movie not found!");

            if (!await _db.Set<Genre>().AnyAsync(
                genre => genre.Id == genreId, cancellationToken))
                return Error("Genre not found!");

            if (await DbQuery().AnyAsync(movieGenre =>
                movieGenre.MovieId == movieId &&
                movieGenre.GenreId == genreId, cancellationToken))
                return Error("MovieGenre already exists!");

            var movieGenreEntity = new MovieGenre
            {
                MovieId = movieId,
                GenreId = genreId
            };

            try
            {
                await DbAddAsync(movieGenreEntity, cancellationToken);
            }
            catch (DbUpdateException)
            {
                // A concurrent request may have inserted the same pair.
                return Error("MovieGenre already exists!");
            }

            return Success(0, "MovieGenre created successfully.");
        }
    }
}
