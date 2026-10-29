using CORE.Domain;
using CORE.Models;
using CORE.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using Movies.APP.Domain;

namespace Movies.APP.Features.Movies
{
    public class MovieUpdateRequest : Record, IRequest<CommandResponse>
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

    public class MovieUpdateHandler : DbService<Movie>,
        IRequestHandler<MovieUpdateRequest, CommandResponse>
    {
        public MovieUpdateHandler(DbContext db) : base(db)
        {
        }

        public async Task<CommandResponse> Handle(
            MovieUpdateRequest request, CancellationToken cancellationToken)
        {
            if ((request.TotalRevenue ?? 0) < 0)
                return Error("TotalRevenue cannot be negative.");

            if (await DbQuery().AnyAsync(m => m.Id != request.Id &&
                m.Name == request.Name.Trim(), cancellationToken))
                return Error("Movie with the same name exists!");

            var movieEntity = await DbSingleAsync(request.Id, cancellationToken);
            if (movieEntity is null)
                return Error("Movie not found!");

            movieEntity.Name = request.Name.Trim();
            movieEntity.ReleaseDate = request.ReleaseDate;
            movieEntity.TotalRevenue = request.TotalRevenue ?? 0;
            movieEntity.DirectorId = request.DirectorId.Value;
            await DbUpdateAsync(movieEntity, cancellationToken);

            return Success(movieEntity.Id, "Movie updated successfully.");
        }
    }
}
