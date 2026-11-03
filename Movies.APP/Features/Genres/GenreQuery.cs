using CORE.Domain;
using CORE.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using Movies.APP.Domain;
using Movies.APP.Features.Movies;

namespace Movies.APP.Features.Genres
{
    public class GenreQueryRequest : Record,
        IRequest<IQueryable<GenreQueryResponse>>
    {
        public string Name { get; set; }
    }

    public class GenreQueryResponse : Record
    {
        public string Name { get; set; }

        public int MovieCountR { get; set; }
    }

    public class GenreQueryHandler : DbService<Genre>,
        IRequestHandler<GenreQueryRequest,
            IQueryable<GenreQueryResponse>>
    {
        public GenreQueryHandler(DbContext db) : base(db)
        {
        }

        protected override IQueryable<Genre> DbQuery()
        {
            return base.DbQuery()
                .Include(genre => genre.MovieGenres)
                .OrderBy(genre => genre.Name);
        }

        public Task<IQueryable<GenreQueryResponse>> Handle(
            GenreQueryRequest request,
            CancellationToken cancellationToken)
        {
            var query = DbQuery();
            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                var namePattern = "(?i)" + Regex.Escape(request.Name.Trim());
                query = query.Where(genre => Regex.IsMatch(genre.Name, namePattern));
            }

            return Task.FromResult(query.Select(genre => new GenreQueryResponse
            {
                Id = genre.Id,
                Name = genre.Name,
                MovieCountR = genre.MovieGenres.Count
            }));
        }
    }

    public class GenreMoviesQueryRequest : Record,
        IRequest<IQueryable<MovieQueryResponse>>
    {
    }

    public class GenreMoviesQueryHandler : DbService<Movie>,
        IRequestHandler<GenreMoviesQueryRequest, IQueryable<MovieQueryResponse>>
    {
        public GenreMoviesQueryHandler(DbContext db) : base(db)
        {
        }

        public Task<IQueryable<MovieQueryResponse>> Handle(
            GenreMoviesQueryRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(
                DbQuery().Where(m => m.MovieGenres.Any(movieGenre => movieGenre.GenreId == request.Id))
                    .Select(m => new MovieQueryResponse
                    {
                        Id = m.Id,
                        Name = m.Name,
                        ReleaseDate = m.ReleaseDate,
                        TotalRevenue = m.TotalRevenue,
                        DirectorId = m.DirectorId
                    }));
        }
    }
}
