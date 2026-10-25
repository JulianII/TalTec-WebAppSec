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

app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/test", () =>
{
    return Results.Ok(new { message = "API works!" });
});

app.MapAuthEndpoints();
app.MapVaultEndpoints();

app.Run();