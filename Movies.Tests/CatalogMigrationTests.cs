using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Movies.APP.Domain;
using Xunit;

namespace Movies.Tests
{
    public class CatalogMigrationTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Fresh_and_upgraded_databases_keep_catalog_data_and_only_catalog_tables(bool upgrade)
        {
            await using var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            await using var db = new MoviesDb(new DbContextOptionsBuilder<MoviesDb>()
                .UseSqlite(connection).Options);

            if (upgrade)
            {
                await db.GetService<IMigrator>().MigrateAsync("20261102064045_AddDirectors");
                using var legacyTables = connection.CreateCommand();
                legacyTables.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' " +
                    "AND name IN ('Users', 'UserRoles', 'Roles', 'Statuses')";
                Assert.Equal(4L, (long)await legacyTables.ExecuteScalarAsync());
            }
            else
            {
                await db.Database.MigrateAsync();
            }

            var director = new Director { FirstName = "Christopher", LastName = "Nolan" };
            var movie = new Movie
            {
                Name = "Inception", Director = director,
                ReleaseDate = new DateTime(2010, 7, 16), TotalRevenue = 825.5m
            };
            var genre = new Genre { Name = "Science Fiction" };
            db.MovieGenres.Add(new MovieGenre { Movie = movie, Genre = genre });
            await db.SaveChangesAsync();

            await db.Database.MigrateAsync();
            db.ChangeTracker.Clear();

            var storedDirector = Assert.Single(await db.Directors.ToListAsync());
            Assert.Equal(director.Id, storedDirector.Id);
            Assert.Equal("Nolan", storedDirector.LastName);
            var storedMovie = Assert.Single(await db.Movies.ToListAsync());
            Assert.Equal(movie.Id, storedMovie.Id);
            Assert.Equal("Inception", storedMovie.Name);
            Assert.Equal(director.Id, storedMovie.DirectorId);
            Assert.Equal(movie.ReleaseDate, storedMovie.ReleaseDate);
            Assert.Equal(825.5m, storedMovie.TotalRevenue);
            var storedGenre = Assert.Single(await db.Genres.ToListAsync());
            Assert.Equal(genre.Id, storedGenre.Id);
            Assert.Equal("Science Fiction", storedGenre.Name);
            var storedLink = Assert.Single(await db.MovieGenres.ToListAsync());
            Assert.Equal(movie.Id, storedLink.MovieId);
            Assert.Equal(genre.Id, storedLink.GenreId);
            Assert.Empty(await db.Database.GetPendingMigrationsAsync());

            using var tablesCommand = connection.CreateCommand();
            tablesCommand.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' ORDER BY name";
            var tables = new List<string>();
            await using (var reader = await tablesCommand.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                    tables.Add(reader.GetString(0));
            }
            Assert.Equal(new[]
            {
                "Directors", "Genres", "MovieGenres", "Movies", "__EFMigrationsHistory", "sqlite_sequence"
            }, tables);

            using var foreignKeysCommand = connection.CreateCommand();
            foreignKeysCommand.CommandText = "PRAGMA foreign_key_check";
            await using var violations = await foreignKeysCommand.ExecuteReaderAsync();
            Assert.False(await violations.ReadAsync());
        }
    }
}
