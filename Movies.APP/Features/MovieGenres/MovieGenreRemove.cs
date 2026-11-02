using CORE.Domain;
using CORE.Models;
using CORE.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Movies.APP.Domain;

namespace Movies.APP.Features.MovieGenres
{
    public class MovieGenreRemoveRequest : Record, IRequest<CommandResponse>
    {
        public int MovieId { get; set; }

        public int GenreId { get; set; }
    }

    public class MovieGenreRemoveHandler : DbService<MovieGenre>,
        IRequestHandler<MovieGenreRemoveRequest, CommandResponse>
    {
        public MovieGenreRemoveHandler(DbContext db) : base(db)
        {
        }

        public async Task<CommandResponse> Handle(
            MovieGenreRemoveRequest request, CancellationToken cancellationToken)
        {
            var movieGenreEntity = await DbSingleAsync(movieGenre =>
                movieGenre.MovieId == request.MovieId &&
                movieGenre.GenreId == request.GenreId,
                cancellationToken);

            if (movieGenreEntity is null)
                return Error("MovieGenre not found!");

            await DbRemoveAsync(movieGenreEntity, cancellationToken);

            return Success(0, "MovieGenre deleted successfully.");
        }
    }
}
