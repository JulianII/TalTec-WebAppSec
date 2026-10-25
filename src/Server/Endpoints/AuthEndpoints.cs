/*
* This class maps the API Endpoints for login, logout, register and a "me" functionality
*/

using System.Buffers;
using System.Data.Common;
using Microsoft.OpenApi;
using System.Text;
using System.Security.Cryptography;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints (this WebApplication app)
    {
        // Handles Users-Login requests and returns respective IResults
        app.MapPost("/api/auth/login", (
            RequestLogin request,
            AuthenticationService authenticationService,
            SessionManager sessionManager,
            HttpResponse response,
            ILoggerFactory loggerFactory) =>
        {
            ILogger logger = loggerFactory.CreateLogger("AuthEndpoints.Login");

            // Create user objekt and check success
            AuthenticatedUser? user = authenticationService.LoginUser(request);
            if (user == null)
            {
                logger.LogWarning("Failed login for user: {username} at {time}", request.Username, DateTime.UtcNow);
                return Results.Unauthorized();
            }

            // Create a session for the requested user and check validity
            ResultSession? session = sessionManager.CreateSession(user, request.Password);
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
                SameSite = SameSiteMode.None,
                Path = "/",
                Expires = session.ExpiresAt
            });

            logger.LogInformation("Successful Login for {username} at {time}", request.Username, DateTime.UtcNow);

            return Results.Ok(user);
        });

        app.MapPost("/api/auth/register", (
            RequestRegistration request,
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

            AuthenticatedUser? authenticated = authenticationService.RegisterUser(request);
            if(authenticated == null) return Results.Unauthorized();

            // Create Session for created user
            ResultSession? session = sessionManager.CreateSession(authenticated, request.Password);
            if (session == null)
            {
                logger.LogWarning("Failed to create session for user: {username} at {time}", request.Username, DateTime.UtcNow);
                return Results.Problem();
            }

            // Validate the created session and check that it belongs to the authenticated user
            if(sessionManager.ValidateSession(session.Credential)?.UserID != authenticated.UserID)
            {
                logger.LogWarning("Failed to validate session for user: {username} at {time}", request.Username, DateTime.UtcNow);
                return Results.Problem();
            }

            // Send Session-Cookie to user      
            response.Cookies.Append("session", session.Credential, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Path = "/",
                Expires = session.ExpiresAt
            });

            logger.LogInformation("Successful registration for {username} at {time}", request.Username, DateTime.UtcNow);
            return Results.Ok(authenticated);
        });


        // Temporary API testing endpoint for session validation
        app.MapGet("/api/auth/me", (
            HttpRequest request,
            SessionManager sessionManager,
            StorageService storageService,
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

            User? databaseUser = storageService.GetUser(user.UserID);
            if (databaseUser == null)
            {
                logger.LogWarning("Session user {userID} was not found", user.UserID);
                return Results.Unauthorized();
            }

            return Results.Ok(new
            {
                user.UserID,
                databaseUser.Username,
                user.AuthenticationLevel,
                databaseUser.TotpEnabled
            });
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

            // revoke session
            if (!sessionManager.RevokeSession(cookieCredential))
            {
                logger.LogWarning("Failed to revoke session at {time}", DateTime.UtcNow);
                return Results.Problem();
            }

            response.Cookies.Delete("session");
            return Results.Ok();
        });

        app.MapPost("/api/auth/upgrade", (
            RequestMfaUpgrade requestMfa,
            SessionManager sessionManager,
            AuthenticationService authenticationService,
            StorageService storageService,
            HttpRequest request,
            HttpResponse response,
            ILoggerFactory loggerFactory) =>
        {
            ILogger logger = loggerFactory.CreateLogger("AuthEndpoints.Upgrade");

            string? cookieCredential = request.Cookies["session"];
            if(string.IsNullOrEmpty(cookieCredential))
            {
                logger.LogWarning("Upgrade failed: No session cookie at {time}", DateTime.UtcNow);
                return Results.Unauthorized();
            }

            AuthenticatedUser? user = sessionManager.ValidateSession(cookieCredential);
            if(user == null)
            {
                logger.LogWarning("Upgrade failed: Invalid session at {time}", DateTime.UtcNow);
                return Results.Unauthorized();
            }

            User? databaseUser = storageService.GetUser(user.UserID);
            if(databaseUser == null || !databaseUser.TotpEnabled || string.IsNullOrEmpty(databaseUser.TotpSecret))
            {
                logger.LogWarning("Upgrade failed: User {userID} has no TOTP enabled", user.UserID);
                return Results.BadRequest("MFA not enabled for this user");
            }

            if(!authenticationService.VerifyCode(databaseUser.TotpSecret, requestMfa.Code))
            {
                logger.LogWarning("Upgrade failed: Invalid TOTP code for user {userID}", user.UserID);
                return Results.BadRequest("Invalid MFA code");
            }

            if(!sessionManager.UpgradeSession(cookieCredential))
            {
                logger.LogWarning("Upgrade failed: Could not upgrade session for user {userID}", user.UserID);
                return Results.Problem();
            }

            logger.LogInformation("Successfully upgraded session for user {userID} at {time}", user.UserID, DateTime.UtcNow);
            return Results.Ok();
        });

        app.MapGet("/api/auth/totp/setup", (
            SessionManager sessionManager,
            AuthenticationService authenticationService,
            StorageService storageService,
            HttpRequest request,
            ILoggerFactory loggerFactory) =>
        {
            ILogger logger = loggerFactory.CreateLogger("AuthEndpoints.TotpSetup");

            string? cookieCredential = request.Cookies["session"];
            if(string.IsNullOrEmpty(cookieCredential))
            {
                logger.LogWarning("TOTP setup failed: No session cookie at {time}", DateTime.UtcNow);
                return Results.Unauthorized();
            }

            AuthenticatedUser? user = sessionManager.ValidateSession(cookieCredential);
            if(user == null)
            {
                logger.LogWarning("TOTP setup failed: Invalid session at {time}", DateTime.UtcNow);
                return Results.Unauthorized();
            }

            User? databaseUser = storageService.GetUser(user.UserID);
            if(databaseUser == null)
            {
                logger.LogWarning("TOTP setup failed: User not found for ID {userID}", user.UserID);
                return Results.NotFound();
            }

            if (user.AuthenticationLevel != AuthenticationLevel.Reduced || databaseUser.TotpEnabled)
            {
                logger.LogWarning("TOTP setup is not available for user {userID}", user.UserID);
                return Results.Conflict("TOTP is already enabled or the session is not eligible for setup");
            }

            // Generate a new TOTP secret
            string secret = authenticationService.GenerateSecret();
            string qrCodeUrl = authenticationService.GetQrCodeUrl(secret, databaseUser.Username);

            logger.LogInformation("TOTP setup initiated for user {userID} at {time}", user.UserID, DateTime.UtcNow);
            
            return Results.Ok(new ResponseTotpSetup
            {
                Secret = secret,
                QrCodeUrl = qrCodeUrl
            });
        });

        app.MapPost("/api/auth/totp/enable", (
            RequestTotpEnable requestTotp,
            SessionManager sessionManager,
            AuthenticationService authenticationService,
            StorageService storageService,
            HttpRequest request,
            ILoggerFactory loggerFactory) =>
        {
            ILogger logger = loggerFactory.CreateLogger("AuthEndpoints.TotpEnable");

            string? cookieCredential = request.Cookies["session"];
            if(string.IsNullOrEmpty(cookieCredential))
            {
                logger.LogWarning("TOTP enable failed: No session cookie at {time}", DateTime.UtcNow);
                return Results.Unauthorized();
            }

            AuthenticatedUser? user = sessionManager.ValidateSession(cookieCredential);
            if(user == null)
            {
                logger.LogWarning("TOTP enable failed: Invalid session at {time}", DateTime.UtcNow);
                return Results.Unauthorized();
            }

            User? databaseUser = storageService.GetUser(user.UserID);
            if(databaseUser == null)
            {
                logger.LogWarning("TOTP enable failed: User not found for ID {userID}", user.UserID);
                return Results.NotFound();
            }

            if (user.AuthenticationLevel != AuthenticationLevel.Reduced || databaseUser.TotpEnabled)
            {
                logger.LogWarning("TOTP enable is not available for user {userID}", user.UserID);
                return Results.Conflict("TOTP is already enabled or the session is not eligible for setup");
            }

            // Verify the code matches the secret
            if(!authenticationService.VerifyCode(requestTotp.Secret, requestTotp.Code))
            {
                logger.LogWarning("TOTP enable failed: Invalid code for user {userID}", user.UserID);
                return Results.BadRequest("Invalid TOTP code");
            }

            // Save the secret and enable TOTP for the user
            databaseUser.TotpSecret = requestTotp.Secret;
            databaseUser.TotpEnabled = true;
            
            if(!storageService.SaveUserChanges(databaseUser))
            {
                logger.LogWarning("TOTP enable failed: Could not save TOTP settings for user {userID}", user.UserID);
                return Results.Problem();
            }

            if (!sessionManager.UpgradeSession(cookieCredential))
            {
                logger.LogWarning("TOTP was enabled but session upgrade failed for user {userID}", user.UserID);
                return Results.Problem();
            }

            logger.LogInformation("TOTP enabled for user {userID} at {time}", user.UserID, DateTime.UtcNow);
            return Results.Ok();
        });
    } 
}