using CORE.Domain;
using CORE.Models;
using CORE.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using Movies.APP.Domain;

namespace Movies.APP.Features.Movies
{
    public class MovieAddRequest : Record, IRequest<CommandResponse>
    {
        [Required(ErrorMessage = "{0} is required!")]
        public string Name { get; set; }

        public DateTime? ReleaseDate { get; set; }

        public decimal? TotalRevenue { get; set; }

        [Required(ErrorMessage = "{0} is required!")]
        [Range(1, int.MaxValue,
            ErrorMessage = "{0} must be positive!")]
        public int? DirectorId { get; set; }
    }

    public class MovieAddHandler : DbService<Movie>,
        IRequestHandler<MovieAddRequest, CommandResponse>
    {
        public MovieAddHandler(DbContext db) : base(db)
        {

        }

        public async Task<CommandResponse> Handle(
            MovieAddRequest request, CancellationToken cancellationToken)
        {
            if ((request.TotalRevenue ?? 0) < 0)
                return Error("TotalRevenue cannot be negative.");

            if (await DbQuery().AnyAsync(m =>
                m.Name == request.Name.Trim(), cancellationToken
            ))
                return Error("Movie with the same name exists!");

            var movieEntity = new Movie
            {
                Name = request.Name.Trim(),
                ReleaseDate = request.ReleaseDate,
                TotalRevenue = request.TotalRevenue ?? 0,
                DirectorId = request.DirectorId.Value,
            };
            await DbAddAsync(movieEntity, cancellationToken);

            return Success(movieEntity.Id, "Movie created successfully.");
        }
    }
}
