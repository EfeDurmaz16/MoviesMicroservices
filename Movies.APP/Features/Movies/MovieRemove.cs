using CORE.Domain;
using CORE.Models;
using CORE.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Movies.APP.Domain;

namespace Movies.APP.Features.Movies
{
    public class MovieRemoveRequest : Record, IRequest<CommandResponse>
    {
    }

    public class MovieRemoveHandler : DbService<Movie>,
        IRequestHandler<MovieRemoveRequest, CommandResponse>
    {
        public MovieRemoveHandler(DbContext db) : base(db)
        {
        }

        public async Task<CommandResponse> Handle(
            MovieRemoveRequest request, CancellationToken cancellationToken)
        {
            var movieEntity = await DbSingleAsync(request.Id, cancellationToken);
            if (movieEntity is null)
                return Error("Movie not found!");

            await DbRemoveAsync(movieEntity, cancellationToken);

            return Success(movieEntity.Id, "Movie deleted successfully.");
        }
    }
}
