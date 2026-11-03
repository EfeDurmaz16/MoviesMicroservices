using Microsoft.EntityFrameworkCore;

namespace Movies.APP.Domain
{
    public class MoviesDb : DbContext
    {
        public DbSet<Movie> Movies { get; set; }

        public DbSet<Director> Directors { get; set; }

        public DbSet<Genre> Genres { get; set; }

        public DbSet<MovieGenre> MovieGenres { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Movie>()
                .HasOne(movie => movie.Director)
                .WithMany(director => director.Movies)
                .HasForeignKey(movie => movie.DirectorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MovieGenre>(entity =>
            {
                entity.HasKey(movieGenre =>
                    new { movieGenre.MovieId, movieGenre.GenreId });

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

        public MoviesDb(DbContextOptions options) : base(options)
        {
        }
    }
}
