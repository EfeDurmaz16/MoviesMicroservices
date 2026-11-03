using CORE.Domain;
using CORE.Models;
using CORE.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using Movies.APP.Domain;

namespace Movies.APP.Features.Genres
{
    public class GenreAddRequest : Record, IRequest<CommandResponse>
    {
        [Required(ErrorMessage = "{0} is required!")]
        public string Name { get; set; }
    }

    public class GenreAddHandler : DbService<Genre>,
        IRequestHandler<GenreAddRequest, CommandResponse>
    {
        public GenreAddHandler(DbContext db) : base(db)
        {
        }

        public async Task<CommandResponse> Handle(
            GenreAddRequest request,
            CancellationToken cancellationToken)
        {
            var name = request.Name.Trim();

            if (await DbQuery().AnyAsync(
                genre => genre.Name == name,
                cancellationToken))
            {
                return Error("Genre with the same name exists!");
            }

            var genreEntity = new Genre
            {
                Name = name
            };

            await DbAddAsync(genreEntity, cancellationToken);

            return Success(
                genreEntity.Id,
                "Genre created successfully.");
        }
    }
}