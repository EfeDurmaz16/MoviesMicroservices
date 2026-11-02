using Microsoft.EntityFrameworkCore;

namespace Movies.APP.Domain
{
    // Inherits from the Entity Framework's DbContext class for
    // database configurations and operations.
    public class UsersDb : DbContext
    {
        // Represents the Users table in the database.
        public DbSet<User> Users { get; set; }

        // Represents the Roles table in the database.
        public DbSet<Role> Roles { get; set; }

        // Represents the UserRoles table in the database.
        public DbSet<UserRole> UserRoles { get; set; }

        // Represents the Statuses table in the database.
        public DbSet<Status> Statuses { get; set; }

        // Represents the Movies table in the database.
        public DbSet<Movie> Movies { get; set; }

        // Represents the Genres table in the database.
        public DbSet<Genre> Genres { get; set; }

        // Represents the MovieGenres table in the database.
        public DbSet<MovieGenre> MovieGenres { get; set; }

        // Configures the MovieGenre entity with a composite primary key
        // and its relationships to the Movie and Genre entities.
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<MovieGenre>(entity =>
            {
                entity.HasKey(movieGenre =>
                    new { movieGenre.MovieId, movieGenre.GenreId });

                // MovieGenre has no surrogate Id; its key is the pair.
                entity.Ignore(movieGenre => movieGenre.Id);

                entity.HasOne(movieGenre => movieGenre.Movie)
                    .WithMany(movie => movie.MovieGenres)
                    .HasForeignKey(movieGenre => movieGenre.MovieId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(movieGenre => movieGenre.Genre)
                    .WithMany(genre => genre.MovieGenres)
                    .HasForeignKey(movieGenre => movieGenre.GenreId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        // Constructor that takes options of type DbContextOptions
        // parameter and passes it to the base DbContext class constructor.
        // The options parameter is used to configure the database
        // connection with such as the connection string.
        public UsersDb(DbContextOptions options) : base(options)
        {
        }
    }
}
