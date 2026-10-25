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
                string.IsNullOrWhiteSpace(request.Title) ||
                string.IsNullOrWhiteSpace(request.Website))
            {
                logger.LogWarning("Vault Entry request contains empty required fields.");
                return Results.BadRequest();
            }

            string? credentialHash = httpRequest.Cookies["session"];
            if (string.IsNullOrEmpty(credentialHash))
                return Results.Unauthorized();

            // Verify user access.
            AuthenticatedUser? userAuth = sessionManager.ValidateSession(credentialHash);
            if (userAuth == null)
            {
                logger.LogWarning("Invalid session credential. Will not create Vault entry! ");
                return Results.Unauthorized();
            }

            if (!vaultManager.CreateVaultEntry(credentialHash, userAuth, request))
            {
                logger.LogError("Failed to create vault entry for user {UserID}", userAuth.UserID);
                return Results.Problem();
            }

            return Results.Ok();
        });

        app.MapGet("/api/vault", (
            SessionManager sessionManager,
            VaultManager vaultManager,
            HttpRequest httpRequest,
            ILoggerFactory loggerFactory) =>
        {
            ILogger logger = loggerFactory.CreateLogger("VaultEndpoints.list");
            string? credential = httpRequest.Cookies["session"];
            if (string.IsNullOrEmpty(credential))
                return Results.Unauthorized();

            AuthenticatedUser? userAuth = sessionManager.ValidateSession(credential);
            if (userAuth == null)
            {
                logger.LogWarning("Invalid session credential. Will not list vault entries.");
                return Results.Unauthorized();
            }

            if (userAuth.AuthenticationLevel != AuthenticationLevel.Full)
                return Results.StatusCode(StatusCodes.Status403Forbidden);

            SessionContext? context = sessionManager.GetSessionContext(credential);
            if (context == null)
            {
                logger.LogWarning("Couldn't retrieve Session Context. Will not list vault entries.");
                return Results.Unauthorized();
            }

            List<VaultEntryUnencrypted>? entries = vaultManager.GetVaultEntries(userAuth, context);
            if (entries == null)
            {
                logger.LogError("Failed to list vault entries for user {UserID}", userAuth.UserID);
                return Results.Problem();
            }

            return Results.Ok(entries);
        });

        app.MapGet("/api/vault/{id:int}", (
            int id,
            SessionManager sessionManager,
            VaultManager vaultManager,
            HttpRequest httpRequest,
            ILoggerFactory loggerFactory) =>
        {
            ILogger logger = loggerFactory.CreateLogger("VaultEndpoints.get");
            string? credential = httpRequest.Cookies["session"];
            if (string.IsNullOrEmpty(credential))
                return Results.Unauthorized();

            AuthenticatedUser? userAuth = sessionManager.ValidateSession(credential);
            if (userAuth == null)
            {
                logger.LogWarning("Invalid session credential. Will not get vault entry.");
                return Results.Unauthorized();
            }

            if (userAuth.AuthenticationLevel != AuthenticationLevel.Full)
                return Results.StatusCode(StatusCodes.Status403Forbidden);

            SessionContext? context = sessionManager.GetSessionContext(credential);
            if (context == null)
            {
                logger.LogWarning("Couldn't retrieve Session Context. Will not get vault entry.");
                return Results.Unauthorized();
            }

            VaultEntryUnencrypted? entry = vaultManager.GetVaultEntry(id, userAuth, context);
            if (entry == null)
            {
                return Results.NotFound();
            }

            return Results.Ok(new { message = entry });
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
            string? credential = httpRequest.Cookies["session"];
            if (string.IsNullOrEmpty(credential))
                return Results.Unauthorized();

            AuthenticatedUser? userAuth = sessionManager.ValidateSession(credential);
            if (userAuth == null)
            {
                logger.LogWarning("Invalid session credential. Will not edit vault entry.");
                return Results.Unauthorized();
            }

            if (userAuth.AuthenticationLevel != AuthenticationLevel.Full)
                return Results.StatusCode(StatusCodes.Status403Forbidden);

            if (string.IsNullOrWhiteSpace(update.Title) ||
                string.IsNullOrWhiteSpace(update.Website) ||
                string.IsNullOrWhiteSpace(update.ExternalUsername) ||
                string.IsNullOrWhiteSpace(update.GeneratedPassword))
            {
                logger.LogWarning("Vault entry update contains empty required fields.");
                return Results.BadRequest();
            }

            SessionContext? context = sessionManager.GetSessionContext(credential);
            if (context == null)
            {
                logger.LogWarning("Couldn't retrieve Session Context. Will not edit vault entry.");
                return Results.Unauthorized();
            }

            if (!vaultManager.EditVaultEntry(userAuth, context, update, id))
            {
                logger.LogError("Failed to edit vault entry {EntryID} for user {UserID}", id, userAuth.UserID);
                return Results.Problem();
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
            string? credential = httpRequest.Cookies["session"];
            if (string.IsNullOrEmpty(credential))
                return Results.Unauthorized();

            AuthenticatedUser? userAuth = sessionManager.ValidateSession(credential);
            if (userAuth == null)
            {
                logger.LogWarning("Invalid session credential. Will not delete vault entry.");
                return Results.Unauthorized();
            }

            if (userAuth.AuthenticationLevel != AuthenticationLevel.Full)
                return Results.StatusCode(StatusCodes.Status403Forbidden);

            if(!vaultManager.DeleteVaultEntry(userAuth, id))
            {
                logger.LogInformation("Vault entry {EntryID} was not found for user {UserID}", id, userAuth.UserID);
                return Results.NotFound();
            }

            return Results.Ok();
        });
    }
}