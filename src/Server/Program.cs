using Microsoft.EntityFrameworkCore;
using Server.Data;

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    $"Host=db;Port=5432;" +
    $"Database={builder.Configuration["POSTGRES_DB"] ?? "passwordmanager"};" +
    $"Username={builder.Configuration["POSTGRES_USER"] ?? "passwordmanager"};" +
    $"Password={builder.Configuration["POSTGRES_PASSWORD"] ?? "passwort"}";

builder.Services.AddDbContext<PasswordManagerDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<AuthenticationService>();
builder.Services.AddScoped<StorageService>();
builder.Services.AddSingleton<Cryptographer>();
builder.Services.AddScoped<SessionManager>();
builder.Services.AddScoped<VaultManager>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

// Enable CORS - allow credentials and same-origin requests
app.UseCors(builder => builder
    .WithOrigins("https://localhost:8443", "http://localhost:8080", "http://localhost:5234", "http://localhost")
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials());

app.MapGet("/api/test", () =>
{
    return Results.Ok(new { message = "API works!" });
});

app.MapAuthEndpoints();
app.MapVaultEndpoints();

app.Run();