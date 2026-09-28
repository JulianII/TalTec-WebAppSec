using Microsoft.EntityFrameworkCore;
using Server.Data;

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    $"Host=db;Port=5432;" +
    $"Database={builder.Configuration["POSTGRES_DB"]};" +
    $"Username={builder.Configuration["POSTGRES_USER"]};" +
    $"Password={builder.Configuration["POSTGRES_PASSWORD"]}";

builder.Services.AddDbContext<PasswordManagerDbContext>(options =>
    options.UseNpgsql(connectionString));

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/test", () =>
{
    return Results.Ok(new { message = "API works!" });
});

app.Run();