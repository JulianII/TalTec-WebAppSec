/*
* This class maps the API Endpoints for login, logout, register and a "me" functionality
*/

using System.Buffers;
using Microsoft.OpenApi;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints (this WebApplication app)
    {
        // Handles Users-Login requests and returns respective IResults
        app.MapPost("/api/auth/login", (
            LoginRequest request,
            AuthenticationService authenticationService,
            SessionManager sessionManager,
            HttpResponse response,
            ILoggerFactory loggerFactory) =>
        {
            ILogger logger = loggerFactory.CreateLogger("AuthEndpoints.Login");

            // Create user objekt and check success
            AuthenticatedUser? user = authenticationService.Login(request);
            if (user == null)
            {
                logger.LogWarning("Failed login for user: {username} at {time}", request.Username, DateTime.UtcNow);
                return Results.Unauthorized();
            }

            // Create a session for the requested user and check validity
            SessionResult? session = sessionManager.CreateSession(user);
            if (session == null)
            {
                logger.LogWarning("Failed to create session for user: {username} at {time}", request.Username, DateTime.UtcNow);
                return Results.Problem();
            }

            // Validate the created session and check that it belongs to the authenticated user
            if(sessionManager.ValidateSession(session.Credential)?.UserID != user.UserID)
            {
                logger.LogWarning("Failed to validate session for user: {username} at {time}", request.Username, DateTime.UtcNow);
                return Results.Problem();
            }

            // Create session-Cookie and return it to client
            response.Cookies.Append("session", session.Credential, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Path = "/",
                Expires = session.ExpiresAt
            });

            return Results.Ok();
        });


        // Temporary API testing endpoint for session validation
        app.MapGet("/api/auth/me", (
            HttpRequest request,
            SessionManager sessionManager,
            ILoggerFactory loggerFactory) =>
        {
            ILogger logger = loggerFactory.CreateLogger("AuthEndpoints.Me");

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

            return Results.Ok(user);
        });


        // Handles Users-Login requests and returns respective IResults
        app.MapPost("/api/auth/logout", (
            SessionManager sessionManager,
            HttpRequest request,
            HttpResponse response,
            ILoggerFactory loggerFactory) =>
        {
            ILogger logger = loggerFactory.CreateLogger("AuthEndpoints.Logout");
            
            // Get credential from cookie
            string? cookieCredential = request.Cookies["session"];

            // No need to validate session. If the user doesnt have a session we can keep it idempotent this way.

            // revoke session
            if (!sessionManager.RevokeSession(cookieCredential))
            {
                logger.LogWarning("Failed to revoke session at {time}", DateTime.UtcNow);
                return Results.Problem();
            }

            response.Cookies.Delete("session");
            return Results.Ok();
        });
    } 
}