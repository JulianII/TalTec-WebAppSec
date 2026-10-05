public static class VaultEndpoints
{
    public static void MapAuthEndpoints (this WebApplication app)
    {
        // Handles Users-Login requests and returns respective IResults
        app.MapPost("/api/vault/login", (
            CreateVaultEntryRequest request,
            VaultManager vaultManager,
            HttpRequest httpRequest,
            ILoggerFactory loggerFactory) =>
        {
            ILogger logger = loggerFactory.CreateLogger("VaultEndpoints.create");

            string? sessionCredential = httpRequest.Cookies["session"];
            if (sessionCredential == null)
            {
                logger.LogWarning("Missing Session for {username} ", request.Username);
                return Results.Unauthorized();
            }

            if (string.IsNullOrWhiteSpace(request.Title) ||
                string.IsNullOrWhiteSpace(request.Username) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                logger.LogWarning("Vault Entry contains empty required fields.");
                return Results.BadRequest();
            }

            vaultManager.CreateVaultEntry(sessionCredential, request);
            return Results.Ok();
        });
    }
}