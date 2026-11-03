using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Movies.APP.Domain;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<DbContext, MoviesDb>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString(nameof(MoviesDb))));
foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
{
    builder.Services.AddMediatR(
        config => config.RegisterServicesFromAssemblies(assembly));
}

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Movies API", Version = "v1" }));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DbContext>();
    db.Database.Migrate();
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
