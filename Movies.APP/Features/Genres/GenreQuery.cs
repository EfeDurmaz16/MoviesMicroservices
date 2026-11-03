using CORE.Domain;
using CORE.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Movies.APP.Domain;

namespace Movies.APP.Features.Genres
{
    public class GenreQueryRequest : Record,
        IRequest<IQueryable<GenreQueryResponse>>
    {
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
            var query = DbQuery().Select(genre => new GenreQueryResponse
            {
                Id = genre.Id,
                Name = genre.Name,
                MovieCountR = genre.MovieGenres.Count
            });

            return Task.FromResult(query);
        }
    }
}