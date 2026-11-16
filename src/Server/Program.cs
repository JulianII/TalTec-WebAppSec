using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Antiforgery;
using System.Threading.RateLimiting;
using Server.Data;

static string GetRateLimitPartitionKey(HttpContext context) =>
    context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    $"Host=db;Port=5432;" +
    $"Database={builder.Configuration["POSTGRES_DB"] ?? "passwordmanager"};" +
    $"Username={builder.Configuration["POSTGRES_USER"] ?? "passwordmanager"};" +
    $"Password={builder.Configuration["POSTGRES_PASSWORD"] ?? "passwort"}";

builder.Services.AddDbContext<PasswordManagerDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "__Host-csrf";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.Path = "/";
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        var httpContext = context.HttpContext;
        var logger = httpContext.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("RateLimiting");

        logger.LogWarning(
            "Rate limit exceeded for {Method} {Path}. TraceId: {TraceId}",
            httpContext.Request.Method,
            httpContext.Request.Path,
            httpContext.TraceIdentifier);

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            httpContext.Response.Headers["Retry-After"] =
                Math.Ceiling(retryAfter.TotalSeconds).ToString(
                    System.Globalization.CultureInfo.InvariantCulture);
        }

        await Results.Problem(
            statusCode: StatusCodes.Status429TooManyRequests,
            title: "Too many requests. Please try again later.",
            extensions: new Dictionary<string, object?>
            {
                ["traceId"] = httpContext.TraceIdentifier
            }).ExecuteAsync(httpContext);
    };

    options.AddPolicy("login", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            GetRateLimitPartitionKey(context),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    options.AddPolicy("registration", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            GetRateLimitPartitionKey(context),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromHours(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    options.AddPolicy("mfa-verification", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            GetRateLimitPartitionKey(context),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    options.AddPolicy("mfa-setup", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            GetRateLimitPartitionKey(context),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 3,
                Window = TimeSpan.FromHours(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    options.AddPolicy("vault-read", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            GetRateLimitPartitionKey(context),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    options.AddPolicy("vault-write", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            GetRateLimitPartitionKey(context),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    options.AddPolicy("trusted-device", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            GetRateLimitPartitionKey(context),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromDays(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    options.AddPolicy("auth-read", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            GetRateLimitPartitionKey(context),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 120,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

builder.Services.AddScoped<AuthenticationService>();
builder.Services.AddScoped<StorageService>();
builder.Services.AddSingleton<Cryptographer>();
builder.Services.AddScoped<SessionManager>();
builder.Services.AddScoped<VaultManager>();
builder.Services.AddScoped<TrustedDeviceService>();

var app = builder.Build();

app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    var logger = context.RequestServices
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("UnhandledException");

    context.Response.Clear();
    int statusCode;
    string title;
    if (exception is BadHttpRequestException badRequest)
    {
        statusCode = badRequest.StatusCode;
        title = "Invalid request.";
        logger.LogWarning(
            "Rejected invalid request {Method} {Path}. TraceId: {TraceId}",
            context.Request.Method,
            context.Request.Path,
            context.TraceIdentifier);
    }
    else
    {
        statusCode = StatusCodes.Status500InternalServerError;
        title = "An unexpected error occurred.";
        logger.LogError(
            exception,
            "Unhandled exception processing {Method} {Path}. TraceId: {TraceId}",
            context.Request.Method,
            context.Request.Path,
            context.TraceIdentifier);
    }

    await Results.Problem(
        statusCode: statusCode,
        title: title,
        extensions: new Dictionary<string, object?>
        {
            ["traceId"] = context.TraceIdentifier
        }).ExecuteAsync(context);
}));

app.UseHttpsRedirection();
app.UseRouting();
app.UseRateLimiter();
app.UseAntiforgery();
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/test", () =>
{
    return Results.Ok(new { message = "API works!" });
});

app.MapAuthEndpoints();
app.MapVaultEndpoints();
app.MapCryptoEndpoints();

app.Run();