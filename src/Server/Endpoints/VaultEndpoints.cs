public static class VaultEndpoints
{
    public static void MapVaultEndpoints (this WebApplication app)
    {
        app.MapPost("/api/vault", (
            RequestCreateVaultEntry request,
            SessionManager sessionManager,
            VaultManager vaultManager,
            HttpRequest httpRequest,
            HttpResponse response,
            ILoggerFactory loggerFactory) =>
        {
            ILogger logger = loggerFactory.CreateLogger("VaultEndpoints.create");

            string? sessionCredential = httpRequest.Cookies["session"];
            if (sessionCredential == null)
            {
                logger.LogWarning("Missing Session for {username} ", request.ExternalUsername);
                return Results.Unauthorized();
            }

            if (string.IsNullOrWhiteSpace(request.GeneratedPassword) ||
                string.IsNullOrWhiteSpace(request.ExternalUsername) ||
                string.IsNullOrWhiteSpace(request.Website))
            {
                logger.LogWarning("Vault Entry request contains empty required fields.");
                return Results.BadRequest();
            }

            // Verify user access.
            AuthenticatedUser? userAuth = sessionManager.ValidateSession(sessionCredential);
            if (userAuth == null)
            {
                logger.LogWarning("Invalid session credential. Will not create Vault entry! ");
                return Results.Unauthorized();
            }

            vaultManager.CreateVaultEntry(sessionCredential, userAuth, request);
            return Results.Ok();
        });

        app.MapGet("/api/vault", (
            SessionManager sessionManager,
            VaultManager vaultManager,
            HttpRequest httpRequest,
            HttpResponse response,
            ILoggerFactory loggerFactory) =>
        {
            ILogger logger = loggerFactory.CreateLogger("VaultEndpoints.get");

            string? sessionCredential = httpRequest.Cookies["session"];
            if (sessionCredential == null)
            {
                logger.LogWarning("Missing Session ");
                return Results.Unauthorized();
            }

            // Verify user access.
            AuthenticatedUser? userAuth = sessionManager.ValidateSession(sessionCredential);
            if (userAuth == null)
            {
                logger.LogWarning("Invalid session credential. Will not create Vault entry! ");
                return Results.Unauthorized();
            }

            return Results.Ok();
        });
    }
}