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

builder.Services.AddScoped<AuthenticationService>();
builder.Services.AddSingleton<StorageService>(); // <-- ONLY SINGLETON FOR TESTING PURPOSES REMOVE AFTER DEV, please TODO
builder.Services.AddScoped<Cryptographer>();
builder.Services.AddScoped<SessionManager>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/test", () =>
{
    return Results.Ok(new { message = "API works!" });
});

app.MapAuthEndpoints();

app.Run();