using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Movies.APP.Domain
{
    public class MoviesDbFactory : IDesignTimeDbContextFactory<MoviesDb>
    {
        public MoviesDb CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<MoviesDb>();
            optionsBuilder.UseSqlite("Data Source=MoviesDB.db");
            return new MoviesDb(optionsBuilder.Options);
        }
    }
}
