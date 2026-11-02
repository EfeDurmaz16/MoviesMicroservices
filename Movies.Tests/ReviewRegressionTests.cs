using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Movies.APP.Domain;
using Movies.APP.Features.Directors;
using Movies.APP.Features.MovieGenres;
using Movies.APP.Features.Movies;
using Xunit;

namespace Movies.Tests
{
    public class ReviewRegressionTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly UsersDb _db;

        public ReviewRegressionTests()
        {
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();
            _db = new UsersDb(new DbContextOptionsBuilder<UsersDb>()
                .UseSqlite(_connection).Options);
            _db.Database.Migrate();
        }

        public void Dispose()
        {
            _db.Dispose();
            _connection.Dispose();
        }

        private async Task<Movie> AddMovie(string name = "Original", string lastName = "Director")
        {
            var director = new Director { FirstName = "Test", LastName = lastName };
            var movie = new Movie { Name = name, Director = director };
            _db.Movies.Add(movie);
            await _db.SaveChangesAsync();
            return movie;
        }

        [Theory]
        [InlineData("Özel", "ÖZEL", "Öztürk", "ÖZTÜRK")]
        [InlineData("A.B", "A.B", "C.D", "C.D")]
        public async Task Name_filters_match_unicode_and_treat_punctuation_literally(
            string movieName, string movieFilter, string lastName, string directorFilter)
        {
            var expected = await AddMovie(movieName, lastName);
            await AddMovie("AXB", "CXD");
            var movies = await new MovieQueryHandler(_db).Handle(
                new MovieQueryRequest { Name = " " + movieFilter + " " }, default);
            var directors = await new DirectorQueryHandler(_db).Handle(
                new DirectorQueryRequest { LastName = " " + directorFilter + " " }, default);

            Assert.Equal(expected.Id, Assert.Single(await movies.ToListAsync()).Id);
            Assert.Equal(expected.DirectorId, Assert.Single(await directors.ToListAsync()).Id);
        }

        [Fact]
        public async Task Invalid_director_is_rejected_without_adding_or_changing_a_movie()
        {
            var movie = await AddMovie();
            var directorId = movie.DirectorId;
            var added = await new MovieAddHandler(_db).Handle(
                new MovieAddRequest { Name = "Invalid", DirectorId = int.MaxValue }, default);
            var updated = await new MovieUpdateHandler(_db).Handle(
                new MovieUpdateRequest
                {
                    Id = movie.Id, Name = "Changed", DirectorId = int.MaxValue,
                    TotalRevenue = 100, ReleaseDate = new DateTime(2000, 1, 1)
                }, default);

            Assert.False(added.IsSuccessful);
            Assert.Equal("Director not found!", added.Message);
            Assert.False(updated.IsSuccessful);
            Assert.Equal("Director not found!", updated.Message);
            _db.ChangeTracker.Clear();
            var stored = Assert.Single(await _db.Movies.ToListAsync());
            Assert.Equal("Original", stored.Name);
            Assert.Equal(directorId, stored.DirectorId);
            Assert.Equal(0m, stored.TotalRevenue);
            Assert.Null(stored.ReleaseDate);
        }

        [Fact]
        public async Task Migrated_database_restricts_director_deletion_and_has_composite_join_key()
        {
            var movie = await AddMovie();
            var error = await Assert.ThrowsAsync<SqliteException>(() =>
                _db.Database.ExecuteSqlInterpolatedAsync(
                    $"DELETE FROM Directors WHERE Id = {movie.DirectorId}"));
            Assert.Equal(19, error.SqliteErrorCode);
            Assert.True(await _db.Directors.AnyAsync(d => d.Id == movie.DirectorId));
            Assert.True(await _db.Movies.AnyAsync(m => m.Id == movie.Id));
            Assert.NotEmpty(await _db.Database.GetAppliedMigrationsAsync());
            Assert.Empty(await _db.Database.GetPendingMigrationsAsync());

            using var command = _connection.CreateCommand();
            command.CommandText = "PRAGMA table_info(MovieGenres)";
            using var reader = await command.ExecuteReaderAsync();
            var columns = new List<(string Name, long KeyPosition)>();
            while (await reader.ReadAsync())
                columns.Add((reader.GetString(1), reader.GetInt64(5)));
            Assert.Equal(new[] { ("MovieId", 1L), ("GenreId", 2L) }, columns);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Deleting_movie_or_genre_only_cascades_to_its_links(bool deleteMovie)
        {
            var movie = await AddMovie();
            var genre = new Genre { Name = "Drama" };
            _db.MovieGenres.Add(new MovieGenre { Movie = movie, Genre = genre });
            await _db.SaveChangesAsync();
            _db.ChangeTracker.Clear();

            if (deleteMovie)
                await _db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Movies WHERE Id = {movie.Id}");
            else
                await _db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Genres WHERE Id = {genre.Id}");

            Assert.Empty(await _db.MovieGenres.ToListAsync());
            Assert.Equal(!deleteMovie, await _db.Movies.AnyAsync(m => m.Id == movie.Id));
            Assert.Equal(deleteMovie, await _db.Genres.AnyAsync(g => g.Id == genre.Id));
            Assert.True(await _db.Directors.AnyAsync(d => d.Id == movie.DirectorId));
        }

        [Fact]
        public async Task Director_migration_rejects_orphaned_movies_without_deleting_existing_data()
        {
            const string previous = "20261031130726_AddGenresAndMovieGenres";
            await _db.GetService<IMigrator>().MigrateAsync(previous);
            await _db.Database.ExecuteSqlRawAsync(
                "INSERT INTO Movies (Id, Name, TotalRevenue, DirectorId) VALUES (1, 'Existing film', '0', 7)");
            await _db.Database.ExecuteSqlRawAsync("INSERT INTO Genres (Id, Name) VALUES (1, 'Drama')");
            await _db.Database.ExecuteSqlRawAsync("INSERT INTO MovieGenres (MovieId, GenreId) VALUES (1, 1)");

            var error = await Assert.ThrowsAsync<SqliteException>(() => _db.Database.MigrateAsync());

            Assert.Equal(19, error.SqliteErrorCode);
            Assert.Equal("Existing film", (await _db.Movies.SingleAsync()).Name);
            Assert.Single(await _db.MovieGenres.ToListAsync());
            Assert.Equal(previous, (await _db.Database.GetAppliedMigrationsAsync()).Last());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Foreign_key_failure_is_not_reported_as_duplicate_and_update_rolls_back(bool update)
        {
            var movie = await AddMovie();
            var originalGenre = new Genre { Name = "Original" };
            var targetGenre = new Genre { Name = "Target" };
            _db.Genres.AddRange(originalGenre, targetGenre);
            _db.MovieGenres.Add(new MovieGenre { Movie = movie, Genre = originalGenre });
            await _db.SaveChangesAsync();
            _db.ChangeTracker.Clear();
            using var raceDb = new DeleteGenreBeforeSaveDb(
                new DbContextOptionsBuilder<UsersDb>().UseSqlite(_connection).Options)
            {
                GenreToDelete = targetGenre.Id
            };

            if (update)
                await Assert.ThrowsAsync<DbUpdateException>(() =>
                    new MovieGenreUpdateHandler(raceDb).Handle(new MovieGenreUpdateRequest
                    {
                        OldMovieId = movie.Id, OldGenreId = originalGenre.Id,
                        MovieId = movie.Id, GenreId = targetGenre.Id
                    }, default));
            else
                await Assert.ThrowsAsync<DbUpdateException>(() =>
                    new MovieGenreAddHandler(raceDb).Handle(new MovieGenreAddRequest
                    {
                        MovieId = movie.Id, GenreId = targetGenre.Id
                    }, default));

            Assert.False(await _db.Genres.AnyAsync(g => g.Id == targetGenre.Id));
            var link = Assert.Single(await _db.MovieGenres.AsNoTracking().ToListAsync());
            Assert.Equal(movie.Id, link.MovieId);
            Assert.Equal(originalGenre.Id, link.GenreId);
        }

        private sealed class DeleteGenreBeforeSaveDb : UsersDb
        {
            public int GenreToDelete { get; set; }

            public DeleteGenreBeforeSaveDb(DbContextOptions options) : base(options) { }

            public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            {
                // Simulate deletion after existence validation but before the relationship write.
                if (GenreToDelete > 0)
                {
                    var genreId = GenreToDelete;
                    GenreToDelete = 0;
                    await Database.ExecuteSqlInterpolatedAsync(
                        $"DELETE FROM Genres WHERE Id = {genreId}", cancellationToken);
                }
                return await base.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
