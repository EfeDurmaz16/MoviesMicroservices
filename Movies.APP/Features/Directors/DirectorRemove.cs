using CORE.Domain;
using CORE.Models;
using CORE.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Movies.APP.Domain;

namespace Movies.APP.Features.Directors
{
    public class DirectorRemoveRequest : Record, IRequest<CommandResponse>
    {
    }

    public class DirectorRemoveHandler : DbService<Director>,
        IRequestHandler<DirectorRemoveRequest, CommandResponse>
    {
        public DirectorRemoveHandler(DbContext db) : base(db)
        {
        }

        protected override IQueryable<Director> DbQuery()
        {
            return base.DbQuery().Include(d => d.Movies);
        }

        public async Task<CommandResponse> Handle(
            DirectorRemoveRequest request, CancellationToken cancellationToken)
        {
            var directorEntity = await DbSingleAsync(request.Id, cancellationToken);
            if (directorEntity is null)
                return Error("Director not found!");

            if (directorEntity.Movies.Any())
                return Error("Director has related movies!");

            await DbRemoveAsync(directorEntity, cancellationToken);

            return Success(directorEntity.Id, "Director deleted successfully.");
        }
    }
}
