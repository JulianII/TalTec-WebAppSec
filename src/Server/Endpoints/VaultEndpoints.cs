public static class VaultEndpoints
{
    public static void MapVaultEndpoints (this WebApplication app)
    {
        app.MapPost("/api/vault", (
            RequestCreateVaultEntry request,
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
                logger.LogWarning("Vault Entry contains empty required fields.");
                return Results.BadRequest();
            }

            vaultManager.CreateVaultEntry(sessionCredential, request);

            response.Cookies.Delete("session");
            return Results.Ok();
        });
    }
}