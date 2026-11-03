using CORE.Domain;
using CORE.Models;
using CORE.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using Movies.APP.Domain;

namespace Movies.APP.Features.Genres
{
    public class GenreUpdateRequest : Record, IRequest<CommandResponse>
    {
        [Required(ErrorMessage = "{0} is required!")]
        public string Name { get; set; }
    }

    public class GenreUpdateHandler : DbService<Genre>,
        IRequestHandler<GenreUpdateRequest, CommandResponse>
    {
        public GenreUpdateHandler(DbContext db) : base(db)
        {
        }

        public async Task<CommandResponse> Handle(
            GenreUpdateRequest request,
            CancellationToken cancellationToken)
        {
            var name = request.Name.Trim();

            if (await DbQuery().AnyAsync(
                genre => genre.Id != request.Id &&
                    genre.Name == name,
                cancellationToken))
            {
                return Error("Genre with the same name exists!");
            }

            var genreEntity = await DbSingleAsync(
                request.Id,
                cancellationToken);

            if (genreEntity is null)
                return Error("Genre not found!");

            genreEntity.Name = name;

            await DbUpdateAsync(
                genreEntity,
                cancellationToken);

            return Success(
                genreEntity.Id,
                "Genre updated successfully.");
        }
    }
}