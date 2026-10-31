using CORE.Domain;
using CORE.Models;
using CORE.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using Movies.APP.Domain;

namespace Movies.APP.Features.MovieGenres
{
    public class MovieGenreUpdateRequest : Record, IRequest<CommandResponse>
    {
        public int OldMovieId { get; set; }

        public int OldGenreId { get; set; }

        [Required(ErrorMessage = "{0} is required!")]
        [Range(1, int.MaxValue, ErrorMessage = "{0} must be positive!")]
        public int? MovieId { get; set; }

        [Required(ErrorMessage = "{0} is required!")]
        [Range(1, int.MaxValue, ErrorMessage = "{0} must be positive!")]
        public int? GenreId { get; set; }
    }

    public class MovieGenreUpdateHandler : DbService<MovieGenre>,
        IRequestHandler<MovieGenreUpdateRequest, CommandResponse>
    {
        private readonly DbContext _db;

        public MovieGenreUpdateHandler(DbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<CommandResponse> Handle(
            MovieGenreUpdateRequest request, CancellationToken cancellationToken)
        {
            var oldMovieGenreEntity = await DbSingleAsync(movieGenre =>
                movieGenre.MovieId == request.OldMovieId &&
                movieGenre.GenreId == request.OldGenreId,
                cancellationToken);

            if (oldMovieGenreEntity is null)
                return Error("MovieGenre not found!");

            var movieId = request.MovieId.Value;
            var genreId = request.GenreId.Value;

            if (request.OldMovieId == movieId &&
                request.OldGenreId == genreId)
                return Success(0, "MovieGenre updated successfully.");

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

            var newMovieGenreEntity = new MovieGenre
            {
                MovieId = movieId,
                GenreId = genreId
            };

            try
            {
                // Remove the old pair and add the new pair in one
                // SaveChanges call so the composite-key change is atomic.
                await DbRemoveAsync(oldMovieGenreEntity,
                    cancellationToken, save: false);
                await DbAddAsync(newMovieGenreEntity, cancellationToken);
            }
            catch (DbUpdateException)
            {
                return Error("MovieGenre already exists!");
            }

            return Success(0, "MovieGenre updated successfully.");
        }
    }
}
