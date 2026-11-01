using CORE.Domain;
using CORE.Models;
using CORE.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using Movies.APP.Domain;

namespace Movies.APP.Features.Directors
{
    public class DirectorUpdateRequest : Record, IRequest<CommandResponse>
    {
        [Required(ErrorMessage = "{0} is required!")]
        [StringLength(50, ErrorMessage = "{0} must be maximum {1} characters!")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "{0} is required!")]
        [StringLength(50, ErrorMessage = "{0} must be maximum {1} characters!")]
        public string LastName { get; set; }

        public bool IsRetired { get; set; }
    }

    public class DirectorUpdateHandler : DbService<Director>,
        IRequestHandler<DirectorUpdateRequest, CommandResponse>
    {
        public DirectorUpdateHandler(DbContext db) : base(db)
        {
        }

        public async Task<CommandResponse> Handle(
            DirectorUpdateRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.FirstName) ||
                string.IsNullOrWhiteSpace(request.LastName))
                return Error("FirstName and LastName are required!");

            var directorEntity = await DbSingleAsync(request.Id, cancellationToken);
            if (directorEntity is null)
                return Error("Director not found!");

            directorEntity.FirstName = request.FirstName.Trim();
            directorEntity.LastName = request.LastName.Trim();
            directorEntity.IsRetired = request.IsRetired;
            await DbUpdateAsync(directorEntity, cancellationToken);

            return Success(directorEntity.Id, "Director updated successfully.");
        }
    }
}
