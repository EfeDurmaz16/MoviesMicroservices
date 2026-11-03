using CORE.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Movies.API.Controllers;
using Movies.APP.Domain;
using Movies.APP.Features.Directors;
using Xunit;

namespace Movies.Tests
{
    public class DirectorsTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly MoviesDb _db;
        private readonly IMediator _mediator;

        public DirectorsTests()
        {
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<MoviesDb>()
                .UseSqlite(_connection)
                .Options;
            _db = new MoviesDb(options);
            _db.Database.EnsureCreated();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<DbContext>(_db);
            services.AddMediatR(config =>
                config.RegisterServicesFromAssembly(typeof(DirectorAddHandler).Assembly));
            _mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();
        }

        public void Dispose()
        {
            _db.Dispose();
            _connection.Dispose();
        }

        private Task<CommandResponse> AddDirector(
            string firstName, string lastName, bool isRetired = false)
            => _mediator.Send(new DirectorAddRequest
            {
                FirstName = firstName,
                LastName = lastName,
                IsRetired = isRetired
            });

        private Task<IQueryable<DirectorQueryResponse>> QueryDirectors(
            string lastName = null, bool? isRetired = null)
            => _mediator.Send(new DirectorQueryRequest
            {
                LastName = lastName,
                IsRetired = isRetired
            });

        [Fact]
        public async Task Crud_round_trip_returns_created_read_updated_and_deleted_records()
        {
            var created = await AddDirector(" Christopher ", " Nolan ");

            Assert.True(created.IsSuccessful);
            Assert.True(created.Id > 0);

            var read = await (await QueryDirectors())
                .SingleAsync(d => d.Id == created.Id);
            Assert.Equal("Christopher", read.FirstName);
            Assert.Equal("Nolan", read.LastName);
            Assert.False(read.IsRetired);

            var updated = await _mediator.Send(new DirectorUpdateRequest
            {
                Id = created.Id,
                FirstName = "Christopher",
                LastName = "Nolan",
                IsRetired = true
            });
            Assert.True(updated.IsSuccessful);

            var afterUpdate = await (await QueryDirectors())
                .SingleAsync(d => d.Id == created.Id);
            Assert.True(afterUpdate.IsRetired);

            var controller = new DirectorsController(
                NullLogger<DirectorsController>.Instance, _mediator);
            var deleted = await controller.Delete(created.Id, CancellationToken.None);

            Assert.IsType<NoContentResult>(deleted);
            Assert.False(await (await QueryDirectors()).AnyAsync(d => d.Id == created.Id));
        }

        [Fact]
        public async Task Last_name_filter_is_case_insensitive_and_ignores_surrounding_space()
        {
            await AddDirector("Christopher", "Nolan");
            await AddDirector("Denis", "Villeneuve");

            var matches = await (await QueryDirectors(" nOl ")).ToListAsync();

            var director = Assert.Single(matches);
            Assert.Equal("Nolan", director.LastName);
        }

        [Fact]
        public async Task Is_retired_filter_returns_only_the_requested_state()
        {
            await AddDirector("Christopher", "Nolan");
            await AddDirector("Clint", "Eastwood", true);

            var active = await (await QueryDirectors(isRetired: false)).ToListAsync();
            var retired = await (await QueryDirectors(isRetired: true)).ToListAsync();

            Assert.Equal("Nolan", Assert.Single(active).LastName);
            Assert.Equal("Eastwood", Assert.Single(retired).LastName);
        }

        [Theory]
        [InlineData(" ", "Nolan")]
        [InlineData("Christopher", " ")]
        [InlineData(null, "Nolan")]
        [InlineData("Christopher", null)]
        public async Task Add_and_update_reject_missing_names(string firstName, string lastName)
        {
            var added = await AddDirector(firstName, lastName);
            Assert.False(added.IsSuccessful);
            Assert.Equal("FirstName and LastName are required!", added.Message);

            var director = await AddDirector("Christopher", "Nolan");

            var updated = await _mediator.Send(new DirectorUpdateRequest
            {
                Id = director.Id,
                FirstName = firstName,
                LastName = lastName
            });
            Assert.False(updated.IsSuccessful);
            Assert.Equal("FirstName and LastName are required!", updated.Message);
        }

        [Fact]
        public async Task Update_and_delete_return_not_found_for_a_missing_director()
        {
            var updated = await _mediator.Send(new DirectorUpdateRequest
            {
                Id = 999,
                FirstName = "Missing",
                LastName = "Director"
            });
            var deleted = await _mediator.Send(new DirectorRemoveRequest { Id = 999 });

            Assert.Equal("Director not found!", updated.Message);
            Assert.Equal("Director not found!", deleted.Message);
        }

        [Fact]
        public async Task Delete_is_rejected_while_movies_reference_the_director()
        {
            var director = await AddDirector("Christopher", "Nolan");
            _db.Set<Movie>().Add(new Movie
            {
                Name = "Inception",
                DirectorId = director.Id
            });
            await _db.SaveChangesAsync();

            var controller = new DirectorsController(
                NullLogger<DirectorsController>.Instance, _mediator);
            var result = await controller.Delete(director.Id, CancellationToken.None);

            var conflict = Assert.IsType<ConflictObjectResult>(result);
            var response = Assert.IsType<CommandResponse>(conflict.Value);
            Assert.Equal("Director has related movies!", response.Message);
            Assert.True(await _db.Set<Director>().AnyAsync(d => d.Id == director.Id));
        }

        [Fact]
        public async Task Related_movies_query_returns_only_the_given_directors_movies()
        {
            var nolan = await AddDirector("Christopher", "Nolan");
            var villeneuve = await AddDirector("Denis", "Villeneuve");
            _db.Set<Movie>().AddRange(
                new Movie { Name = "Inception", DirectorId = nolan.Id },
                new Movie { Name = "Dune", DirectorId = villeneuve.Id });
            await _db.SaveChangesAsync();

            var movies = await (await _mediator.Send(
                new DirectorMoviesQueryRequest { Id = nolan.Id })).ToListAsync();

            var movie = Assert.Single(movies);
            Assert.Equal("Inception", movie.Name);
            Assert.Equal(nolan.Id, movie.DirectorId);
        }

        [Fact]
        public async Task Controller_returns_not_found_for_a_missing_director()
        {
            var controller = new DirectorsController(
                NullLogger<DirectorsController>.Instance, _mediator);

            Assert.IsType<NotFoundObjectResult>(
                await controller.GetById(999, CancellationToken.None));
            Assert.IsType<NotFoundObjectResult>(
                await controller.GetMovies(999, CancellationToken.None));
        }

        [Fact]
        public async Task Controller_creates_and_reads_a_director()
        {
            var controller = new DirectorsController(
                NullLogger<DirectorsController>.Instance, _mediator);

            var created = await controller.Post(new DirectorAddRequest
            {
                FirstName = "Christopher",
                LastName = "Nolan"
            }, CancellationToken.None);

            var createdAt = Assert.IsType<CreatedAtActionResult>(created);
            var response = Assert.IsType<CommandResponse>(createdAt.Value);

            var read = await controller.GetById(response.Id, CancellationToken.None);
            var ok = Assert.IsType<OkObjectResult>(read);
            var director = Assert.IsType<DirectorQueryResponse>(ok.Value);
            Assert.Equal("Nolan", director.LastName);
        }
    }
}
