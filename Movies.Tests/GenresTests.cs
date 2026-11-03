using CORE.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Movies.API.Controllers;
using Movies.APP.Domain;
using Movies.APP.Features.Genres;
using Movies.APP.Features.Movies;
using Xunit;

namespace Movies.Tests
{
    public class GenresTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly UsersDb _db;
        private readonly ServiceProvider _services;
        private readonly IMediator _mediator;
        private readonly GenresController _controller;

        public GenresTests()
        {
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();
            _db = new UsersDb(new DbContextOptionsBuilder<UsersDb>().UseSqlite(_connection).Options);
            _db.Database.EnsureCreated();
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<DbContext>(_db);
            services.AddMediatR(config => config.RegisterServicesFromAssembly(typeof(GenreAddHandler).Assembly));
            _services = services.BuildServiceProvider();
            _mediator = _services.GetRequiredService<IMediator>();
            _controller = new GenresController(NullLogger<GenresController>.Instance, _mediator);
        }

        public void Dispose()
        {
            _services.Dispose();
            _db.Dispose();
            _connection.Dispose();
        }

        private Task<CommandResponse> AddGenre(string name)
            => _mediator.Send(new GenreAddRequest { Name = name });

        [Fact]
        public async Task Crud_uses_created_ok_no_content_and_not_found_responses()
        {
            var empty = Assert.IsType<OkObjectResult>(await _controller.Get(new GenreQueryRequest(), default));
            Assert.Empty(Assert.IsType<List<GenreQueryResponse>>(empty.Value));
            var created = Assert.IsType<CreatedAtActionResult>(
                await _controller.Post(new GenreAddRequest { Name = " Drama " }, default));
            var id = Assert.IsType<CommandResponse>(created.Value).Id;
            var read = Assert.IsType<OkObjectResult>(await _controller.GetById(id, default));
            Assert.Equal("Drama", Assert.IsType<GenreQueryResponse>(read.Value).Name);
            Assert.IsType<OkObjectResult>(await _controller.Put(id,
                new GenreUpdateRequest { Id = 999, Name = "Comedy" }, default));
            Assert.Equal("Comedy", (await _db.Genres.SingleAsync()).Name);
            Assert.IsType<NoContentResult>(await _controller.Delete(id, default));
            Assert.IsType<NotFoundObjectResult>(await _controller.GetById(id, default));
            Assert.IsType<NotFoundObjectResult>(await _controller.Delete(id, default));
            Assert.IsType<NotFoundObjectResult>(await _controller.Put(id,
                new GenreUpdateRequest { Name = "Missing" }, default));
        }

        [Fact]
        public async Task Duplicate_names_are_trimmed_and_update_excludes_its_own_id()
        {
            var drama = await AddGenre("Drama");
            var comedy = await AddGenre("Comedy");
            Assert.False((await AddGenre(" Drama ")).IsSuccessful);
            Assert.True((await _mediator.Send(new GenreUpdateRequest
                { Id = drama.Id, Name = " Drama " })).IsSuccessful);
            Assert.False((await _mediator.Send(new GenreUpdateRequest
                { Id = drama.Id, Name = " Comedy " })).IsSuccessful);
            _db.ChangeTracker.Clear();
            Assert.Equal("Drama", (await _db.Genres.SingleAsync(g => g.Id == drama.Id)).Name);
            Assert.Equal("Comedy", (await _db.Genres.SingleAsync(g => g.Id == comedy.Id)).Name);
            Assert.True((await AddGenre("drama")).IsSuccessful);
        }

        [Theory]
        [InlineData("Özel", " ÖZEL ", "Other")]
        [InlineData("A.B", "A.B", "AXB")]
        public async Task Name_filter_is_unicode_case_insensitive_and_literal(string name, string filter, string other)
        {
            var expected = await AddGenre(name);
            await AddGenre(other);
            var query = await _mediator.Send(new GenreQueryRequest { Name = filter });
            Assert.Equal(expected.Id, Assert.Single(await query.ToListAsync()).Id);
            var unfiltered = await _mediator.Send(new GenreQueryRequest { Name = " " });
            Assert.Equal(2, await unfiltered.CountAsync());
        }

        [Fact]
        public async Task Related_movies_and_count_only_include_the_requested_genre()
        {
            var drama = new Genre { Name = "Drama" };
            var comedy = new Genre { Name = "Comedy" };
            var director = new Director { FirstName = "Test", LastName = "Director" };
            var first = new Movie { Name = "First", Director = director };
            var second = new Movie { Name = "Second", Director = director };
            _db.MovieGenres.AddRange(new MovieGenre { Genre = drama, Movie = first },
                new MovieGenre { Genre = comedy, Movie = second });
            await _db.SaveChangesAsync();
            var result = Assert.IsType<OkObjectResult>(await _controller.GetMovies(drama.Id, default));
            Assert.Equal(first.Id, Assert.Single(Assert.IsType<List<MovieQueryResponse>>(result.Value)).Id);
            var genres = await _mediator.Send(new GenreQueryRequest());
            Assert.Equal(1, (await genres.SingleAsync(g => g.Id == drama.Id)).MovieCountR);
            var emptyGenre = await AddGenre("Empty");
            var empty = Assert.IsType<OkObjectResult>(await _controller.GetMovies(emptyGenre.Id, default));
            Assert.Empty(Assert.IsType<List<MovieQueryResponse>>(empty.Value));
            Assert.IsType<NotFoundObjectResult>(await _controller.GetMovies(int.MaxValue, default));
        }

        [Fact]
        public async Task Deleting_a_linked_genre_removes_only_its_links()
        {
            var movie = new Movie { Name = "Shared", Director = new Director { FirstName = "A", LastName = "B" } };
            var drama = new Genre { Name = "Drama" };
            var comedy = new Genre { Name = "Comedy" };
            _db.MovieGenres.AddRange(new MovieGenre { Movie = movie, Genre = drama },
                new MovieGenre { Movie = movie, Genre = comedy });
            await _db.SaveChangesAsync();
            _db.ChangeTracker.Clear();
            Assert.IsType<NoContentResult>(await _controller.Delete(drama.Id, default));
            Assert.False(await _db.Genres.AnyAsync(g => g.Id == drama.Id));
            Assert.True(await _db.Genres.AnyAsync(g => g.Id == comedy.Id));
            Assert.True(await _db.Movies.AnyAsync(m => m.Id == movie.Id));
            var link = Assert.Single(await _db.MovieGenres.ToListAsync());
            Assert.Equal(comedy.Id, link.GenreId);
            Assert.Equal(movie.Id, link.MovieId);
        }
    }
}
