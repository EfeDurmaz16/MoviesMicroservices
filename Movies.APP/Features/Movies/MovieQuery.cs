using CORE.Domain;
using CORE.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using Movies.APP.Domain;

namespace Movies.APP.Features.Movies
{
    public class MovieQueryRequest : Record,
        IRequest<IQueryable<MovieQueryResponse>>
    {
        public string Name { get; set; }

        [Range(1, 9999, ErrorMessage = "{0} must be between {1} and {2}!")]
        public int? ReleaseYear { get; set; }
    }

    public class MovieQueryResponse : Record
    {
        public string Name { get; set; }

        public DateTime? ReleaseDate { get; set; }

        public decimal TotalRevenue { get; set; }

        public int DirectorId { get; set; }
    }

    public class MovieQueryHandler : DbService<Movie>,
        IRequestHandler<MovieQueryRequest, IQueryable<MovieQueryResponse>>
    {
        public MovieQueryHandler(DbContext db) : base(db)
        {
        }

        public Task<IQueryable<MovieQueryResponse>> Handle(
            MovieQueryRequest request, CancellationToken cancellationToken)
        {
            var query = DbQuery();

            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                var name = request.Name.Trim().ToLower();
                query = query.Where(m => m.Name.ToLower().Contains(name));
            }

            if (request.ReleaseYear.HasValue)
            {
                query = query.Where(m => m.ReleaseDate.HasValue &&
                    m.ReleaseDate.Value.Year == request.ReleaseYear.Value);
            }

            return Task.FromResult(
                query.Select(m => new MovieQueryResponse
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
