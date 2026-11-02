# Movies Microservices

CTIS 465 Phase 1 Movie Catalog, adapted from [the instructor's Users example](https://github.com/cagilalsac/UsersMicroservices).

The project keeps the existing CORE, APP, API, DbService and MediatR structure.

## Run

Install the .NET 8 SDK, then run from the solution directory:

```sh
dotnet restore 06_Microservices_Users.sln
dotnet build 06_Microservices_Users.sln
dotnet run --project Movies.API --launch-profile http
```

Open <http://localhost:5223/swagger>. The API applies pending EF Core migrations at startup. The default connection string uses `Movies.API/UsersDB.db` when launched with the command above. The inherited Users schema and database name remain in place.

To use a separate database without changing the tracked file:

```sh
dotnet run --project Movies.API --launch-profile http -- --ConnectionStrings:UsersDb="Data Source=/absolute/path/to/movies-local.db"
```

Create a Director before creating a Movie. Movie Add/Update reject an unknown DirectorId with 400. TotalRevenue defaults to 0; ReleaseDate may be null.

## Current endpoints

- `/api/movies`: CRUD, `name` and `releaseYear` filters.
- `/api/directors`: CRUD, `lastName` and `isRetired` filters.
- `/api/directors/{id}/movies`: the director's movies.
- `/api/movie-genres`: relationship CRUD, `movieId` and `genreId` filters. Detail, update and delete routes use `/{movieId}/{genreId}`.

Genre has an entity and table; its CRUD and related-movie endpoints remain the Genre task. Authentication for the new Movie Catalog endpoints remains outside Phase 1.

## Relationships and migrations

- MovieGenre uses `(MovieId, GenreId)` as its database primary key.
- Deleting a Movie or Genre removes its MovieGenre links, not the other principal entity.
- A Director with related movies cannot be deleted: the API returns 409 and the database foreign key uses RESTRICT.
- Keep the committed migrations and snapshot. Coordinate subsequent migration changes through one integration branch after pulling master.
- The Director migration is verified against a fresh database and the repository's original Users database. On an older development database containing Movies without Directors, the migration fails with a foreign-key error and preserves the existing movies and links. Keep that database as a backup and use the separate-database command above for testing. Existing data needs its real Director records mapped before import; do not invent Directors or delete data automatically.

To generate the next migration, use EF tools matching the application's version:

```sh
dotnet tool install --global dotnet-ef --version 8.0.29
dotnet ef migrations add MigrationName --project Movies.APP --startup-project Movies.API
```

## Verify

```sh
dotnet test 06_Microservices_Users.sln
```

The tests cover Director behavior and the review regressions, including migrated SQLite schema, Unicode filters, relationship constraints and failed relationship updates. They use temporary in-memory databases. Swagger remains the manual HTTP verification path.

See [the ER diagram](docs/er-diagram.md) for the model.
