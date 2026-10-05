/*
* This class maps the API Endpoints for login, logout, register and a "me" functionality
*/

using System.Buffers;
using System.Data.Common;
using Microsoft.OpenApi;
using System.Text;

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

            logger.LogInformation("Successful Login for {username} at {time}", request.Username, DateTime.UtcNow);

            return Results.Ok();
        });

        app.MapPost("/api/auth/register", (
            RegistrationRequest request,
            AuthenticationService authenticationService,
            SessionManager sessionManager,
            HttpResponse response,
            Cryptographer cryptographer,
            StorageService storageService,
            ILoggerFactory loggerFactory) =>
        {
            ILogger logger = loggerFactory.CreateLogger("AuthEndpoints.Register");

            logger.LogInformation("New user tried to register: {username} ", request.Username);

            if(request.Password != request.VerifyPassword)
            {
                logger.LogInformation("Registration failed password consistency. ");
                return Results.BadRequest("Passwords differ");
            }
            PasswordVerifierResult result = cryptographer.CreateArgon2idVerifier(request.Password);

            // create user for database
            User databaseEntry = new User
            {
                // UserID - left blank gets auto gened.
                PasswordVerifier = result.Hash,
                Username = request.Username,
                PasswordSalt = result.Salt
            };

            if (!storageService.CreateUser(databaseEntry))
            {
                logger.LogWarning("Database failed to create user: {username}", request.Username);
                return Results.Problem();
            }

            // TODO: Avoid recalculating Argon2id during auto-login after registration; reuse the authenticated user directly.
            // Create authenticatedUser for created User
            AuthenticatedUser? user = authenticationService.Login(new LoginRequest
            {
                Password = request.Password,
                Username = request.Username   
            });
            if (user == null)
            {
                logger.LogError("Failed to authenticate just created user: {username}", request.Username);
                return Results.Problem();
            }

            // Create Session for created user
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

            // Send Session-Cookie to user      
            response.Cookies.Append("session", session.Credential, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Path = "/",
                Expires = session.ExpiresAt
            });

            logger.LogInformation("Successful registration for {username} at {time}", request.Username, DateTime.UtcNow);
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