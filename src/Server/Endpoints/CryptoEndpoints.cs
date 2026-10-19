public static class CryptoEndpoints
{
    public static void MapCryptoEndpoints (this WebApplication app)
    {
            // Returns randomly generated credential if user is authenticated
            app.MapPost("/api/crypt/generateCred", (
                Cryptographer cryptographer,
                HttpResponse response,
                HttpRequest request,
                SessionManager sessionManager,
                ILoggerFactory loggerFactory) =>
            {
                ILogger logger = loggerFactory.CreateLogger("Crypt.generatedCred");

                // authenticate user
                string? cookie = request.Cookies["session"]; 
                if(cookie == null)
                {
                    logger.LogWarning("Cookie was not found at {time}", DateTime.UtcNow);
                    return Results.Unauthorized();
                }
                
                AuthenticatedUser? user = sessionManager.ValidateSession(cookie);
                if(user == null)
                {
                    logger.LogWarning("Failed to validate session at {time}", DateTime.UtcNow);
                    return Results.Unauthorized();
                }

                // generate credential for external use.
                string password = cryptographer.GeneratePassword(32);
                
                return Results.Ok(password);    
            });
    }
}