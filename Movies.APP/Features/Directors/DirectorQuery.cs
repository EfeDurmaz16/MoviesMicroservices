using CORE.Domain;
using CORE.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Movies.APP.Domain;
using Movies.APP.Features.Movies;

namespace Movies.APP.Features.Directors
{
    public class DirectorQueryRequest : Record,
        IRequest<IQueryable<DirectorQueryResponse>>
    {
        public string LastName { get; set; }

        public bool? IsRetired { get; set; }
    }

    public class DirectorQueryResponse : Record
    {
        public string FirstName { get; set; }

        public string LastName { get; set; }

        public bool IsRetired { get; set; }
    }

    public class DirectorQueryHandler : DbService<Director>,
        IRequestHandler<DirectorQueryRequest, IQueryable<DirectorQueryResponse>>
    {
        public DirectorQueryHandler(DbContext db) : base(db)
        {
        }

        public Task<IQueryable<DirectorQueryResponse>> Handle(
            DirectorQueryRequest request, CancellationToken cancellationToken)
        {
            var query = DbQuery();

            if (!string.IsNullOrWhiteSpace(request.LastName))
            {
                var lastName = request.LastName.Trim().ToLower();
                query = query.Where(d => d.LastName.ToLower().Contains(lastName));
            }

            if (request.IsRetired.HasValue)
                query = query.Where(d => d.IsRetired == request.IsRetired.Value);

            return Task.FromResult(
                query.Select(d => new DirectorQueryResponse
                {
                    Id = d.Id,
                    FirstName = d.FirstName,
                    LastName = d.LastName,
                    IsRetired = d.IsRetired
                }));
        }
    }

    public class DirectorMoviesQueryRequest : Record,
        IRequest<IQueryable<MovieQueryResponse>>
    {
    }

    public class DirectorMoviesQueryHandler : DbService<Movie>,
        IRequestHandler<DirectorMoviesQueryRequest, IQueryable<MovieQueryResponse>>
    {
        public DirectorMoviesQueryHandler(DbContext db) : base(db)
        {
        }

        public Task<IQueryable<MovieQueryResponse>> Handle(
            DirectorMoviesQueryRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(
                DbQuery().Where(m => m.DirectorId == request.Id)
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
