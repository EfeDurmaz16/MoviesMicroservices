using CORE.Domain;
using CORE.Models;
using CORE.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Movies.APP.Domain;

namespace Movies.APP.Features.Genres
{
    public class GenreRemoveRequest : Record, IRequest<CommandResponse>
    {
    }

    public class GenreRemoveHandler : DbService<Genre>,
        IRequestHandler<GenreRemoveRequest, CommandResponse>
    {
        public GenreRemoveHandler(DbContext db) : base(db)
        {
        }

        protected override IQueryable<Genre> DbQuery()
        {
            return base.DbQuery()
                .Include(genre => genre.MovieGenres);
        }

        public async Task<CommandResponse> Handle(
            GenreRemoveRequest request,
            CancellationToken cancellationToken)
        {
            var genreEntity = await DbSingleAsync(
                request.Id,
                cancellationToken);

            if (genreEntity is null)
                return Error("Genre not found!");

            if (genreEntity.MovieGenres.Any())
                return Error("Genre has related movies!");

            await DbRemoveAsync(
                genreEntity,
                cancellationToken);

            return Success(
                genreEntity.Id,
                "Genre deleted successfully.");
        }
    }
}