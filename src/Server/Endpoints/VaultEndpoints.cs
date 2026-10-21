public static class VaultEndpoints
{
    public static void MapVaultEndpoints (this WebApplication app)
    {
        app.MapPost("/api/vault", (
            RequestCreateVaultEntry request,
            SessionManager sessionManager,
            VaultManager vaultManager,
            HttpRequest httpRequest,
            ILoggerFactory loggerFactory) =>
        {
            ILogger logger = loggerFactory.CreateLogger("VaultEndpoints.create");

            if (string.IsNullOrWhiteSpace(request.GeneratedPassword) ||
                string.IsNullOrWhiteSpace(request.ExternalUsername) ||
                string.IsNullOrWhiteSpace(request.Website))
            {
                logger.LogWarning("Vault Entry request contains empty required fields.");
                return Results.BadRequest();
            }

            string? credentialHash = httpRequest.Cookies["session"];

            // Verify user access.
            AuthenticatedUser? userAuth = sessionManager.ValidateSession(credentialHash);
            if (userAuth == null)
            {
                logger.LogWarning("Invalid session credential. Will not create Vault entry! ");
                return Results.Unauthorized();
            }

            // If CredentialHash is Null, it gets caught inside ValidateSession() above.
            vaultManager.CreateVaultEntry(credentialHash, userAuth, request);
            return Results.Ok();
        });

        app.MapGet("/api/vault/{ID}", (
            int id,
            SessionManager sessionManager,
            VaultManager vaultManager,
            HttpRequest httpRequest,
            HttpResponse response,
            ILoggerFactory loggerFactory) =>
        {
            ILogger logger = loggerFactory.CreateLogger("VaultEndpoints.get");

            // Verify user access.
            AuthenticatedUser? userAuth = sessionManager.ValidateSession(httpRequest.Cookies["session"]);
            if (userAuth == null)
            {
                logger.LogWarning("Invalid session credential. Will not get Vault entry! ");
                return Results.Unauthorized();
            }

            SessionContext? context = sessionManager.GetSessionContext(httpRequest.Cookies["session"]);
            if (context == null)
            {
                logger.LogWarning("Couldn't retrieve Session Context. Will not get Vault entry! ");
                return Results.Unauthorized();
            }

            VaultEntryUnencrypted? entryUnencrypted = vaultManager.GetVaultEntry(id, userAuth, context);
            if(entryUnencrypted == null)
            {
                logger.LogWarning("Couldn't retrieve vault-entry. Will not get Vault entry! ");
                return Results.Unauthorized();
            }

            return Results.Ok(new { message = entryUnencrypted });
        });

        app.MapPut("/api/vault/{id}", (
            int id,
            VaultEntryUpdate update,
            SessionManager sessionManager,
            VaultManager vaultManager,
            HttpRequest httpRequest,
            ILoggerFactory loggerFactory) =>
        {
            ILogger logger = loggerFactory.CreateLogger("VaultEndpoints.put");

            // Verify user access.
            AuthenticatedUser? userAuth = sessionManager.ValidateSession(httpRequest.Cookies["session"]);
            if (userAuth == null)
            {
                logger.LogWarning("Invalid session credential. Will not edit Vault entry! ");
                return Results.Unauthorized();
            }

            if(!vaultManager.EditVaultEntry(userAuth, update, id))
            {
                logger.LogError("Failed to Edit Fault.");
                return Results.BadRequest();
            }

            return Results.Ok();
        });

        app.MapDelete("/api/vault/{id}", (
            int id,
            SessionManager sessionManager,
            VaultManager vaultManager,
            HttpRequest httpRequest,
            ILoggerFactory loggerFactory) =>
        {
            ILogger logger = loggerFactory.CreateLogger("VaultEndpoints.delete");

            // Verify user access.
            AuthenticatedUser? userAuth = sessionManager.ValidateSession(httpRequest.Cookies["session"]);
            if (userAuth == null)
            {
                logger.LogWarning("Invalid session credential. Will not edit Vault entry! ");
                return Results.Unauthorized();
            }

            if(!vaultManager.DeleteVaultEntry(userAuth, id))
            {
                logger.LogError("Failed to delete vault.");
                return Results.BadRequest();
            }

            return Results.Ok();
        });
    }
}