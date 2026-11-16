using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Diagnostics;
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

app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    var logger = context.RequestServices
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("UnhandledException");

    logger.LogError(
        exception,
        "Unhandled exception processing {Method} {Path}. TraceId: {TraceId}",
        context.Request.Method,
        context.Request.Path,
        context.TraceIdentifier);

    context.Response.Clear();
    await Results.Problem(
        statusCode: StatusCodes.Status500InternalServerError,
        title: "An unexpected error occurred.",
        extensions: new Dictionary<string, object?>
        {
            ["traceId"] = context.TraceIdentifier
        }).ExecuteAsync(context);
}));

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