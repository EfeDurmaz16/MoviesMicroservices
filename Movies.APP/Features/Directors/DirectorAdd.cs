using CORE.Domain;
using CORE.Models;
using CORE.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using Movies.APP.Domain;

namespace Movies.APP.Features.Directors
{
    public class DirectorAddRequest : Record, IRequest<CommandResponse>
    {
        [Required(ErrorMessage = "{0} is required!")]
        [StringLength(50, ErrorMessage = "{0} must be maximum {1} characters!")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "{0} is required!")]
        [StringLength(50, ErrorMessage = "{0} must be maximum {1} characters!")]
        public string LastName { get; set; }

        public bool IsRetired { get; set; }
    }

    public class DirectorAddHandler : DbService<Director>,
        IRequestHandler<DirectorAddRequest, CommandResponse>
    {
        public DirectorAddHandler(DbContext db) : base(db)
        {
        }

        public async Task<CommandResponse> Handle(
            DirectorAddRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.FirstName) ||
                string.IsNullOrWhiteSpace(request.LastName))
                return Error("FirstName and LastName are required!");

            var directorEntity = new Director
            {
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                IsRetired = request.IsRetired
            };
            await DbAddAsync(directorEntity, cancellationToken);

            return Success(directorEntity.Id, "Director created successfully.");
        }
    }
}
