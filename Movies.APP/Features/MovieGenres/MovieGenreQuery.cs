using CORE.Domain;
using CORE.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using Movies.APP.Domain;

namespace Movies.APP.Features.MovieGenres
{
    public class MovieGenreQueryRequest : Record,
        IRequest<IQueryable<MovieGenreQueryResponse>>
    {
        [Range(1, int.MaxValue, ErrorMessage = "{0} must be positive!")]
        public int? MovieId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "{0} must be positive!")]
        public int? GenreId { get; set; }
    }

    public class MovieGenreQueryResponse
    {
        public int MovieId { get; set; }

        public int GenreId { get; set; }
    }

    public class MovieGenreQueryHandler : DbService<MovieGenre>,
        IRequestHandler<MovieGenreQueryRequest,
            IQueryable<MovieGenreQueryResponse>>
    {
        public MovieGenreQueryHandler(DbContext db) : base(db)
        {
        }

        public Task<IQueryable<MovieGenreQueryResponse>> Handle(
            MovieGenreQueryRequest request, CancellationToken cancellationToken)
        {
            var query = DbQuery();

            if (request.MovieId.HasValue)
                query = query.Where(movieGenre =>
                    movieGenre.MovieId == request.MovieId.Value);

            if (request.GenreId.HasValue)
                query = query.Where(movieGenre =>
                    movieGenre.GenreId == request.GenreId.Value);

            return Task.FromResult(
                query.Select(movieGenre => new MovieGenreQueryResponse
                {
                    MovieId = movieGenre.MovieId,
                    GenreId = movieGenre.GenreId
                }));
        }
    }
}
