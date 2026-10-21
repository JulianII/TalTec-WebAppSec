using System.Collections.Concurrent;

public class SessionManager (
        StorageService storageService,
        Cryptographer cryptographer,
        ILogger<SessionManager> logger
    )
{
    private readonly ConcurrentDictionary<string, SessionContext> sessionContexts = new();

    private const int SessionExpiryTimeMinutes = 5;

    public ResultSession? CreateSession (AuthenticatedUser authenticatedUser, string masterPassword)
    {
        DateTime currentTime = DateTime.UtcNow;
        DateTime expiry = currentTime.AddMinutes(SessionExpiryTimeMinutes);

        string credential = cryptographer.GenerateSessionCredential();

        // Create Raw Session Credentials
        SessionCredential session = new()
        {
            CredentialHash = cryptographer.HashSessionCredential(credential),
            UserID = authenticatedUser.UserID,
            AuthLevel = authenticatedUser.AuthenticationLevel,
            CreatedAt = currentTime,
            ExpiresAt = expiry,
            Revoked = false
        };

        // Create Session Entry into Database
        if (!storageService.CreateSessionEntry(session)) return null;  

        // Create Session Result object
        ResultSession result = new()
        {
            Credential = credential,
            ExpiresAt = expiry
        };

        // get VaultSalt
        byte[]? vaultSalt = storageService.GetVaultSalt(authenticatedUser.UserID);
        if(vaultSalt == null) 
        {
            logger.LogError("Unable to find VaultSalt for ID: {ID}", authenticatedUser.UserID);
            return null;
        }

        // Create SessionContext
        SessionContext context = new()
        {
            AuthLevel = session.AuthLevel,
            UserID = session.UserID,
            CredentialHash = session.CredentialHash,
            VaultKey = cryptographer.Argon2idHash(masterPassword, vaultSalt)
        };

        // Add context to dictionary ( only im RAM )
        if (!sessionContexts.TryAdd(session.CredentialHash, context))
        {
            logger.LogWarning("Duplicate session context");
            storageService.SetRevokeSessionFlag(session.CredentialHash);
            return null;
        }

        logger.LogInformation("SessionContext created for ID: {ID}", authenticatedUser.UserID);

        return result;
    }

    public AuthenticatedUser? ValidateSession (string? credential)
    {
        if(String.IsNullOrEmpty(credential))
        {
            logger.LogError("Won't validate Session. Session credential is missing.");
            return null;
        }
        // Transform raw credential
        String hash = cryptographer.HashSessionCredential(credential);

        // Search for active session.
        SessionCredential? activeSession = storageService.GetSessionCredentialByCredentialHash (hash);
        if (activeSession == null) 
        { 
            logger.LogWarning("Session validation failed for UserID: {UserID}", activeSession?.UserID);
            return null; 
        }

        // Compare active-sessions (from database) hash with user-sent credential-hash
        if (
            activeSession.Revoked == false &&
            activeSession.ExpiresAt > DateTime.UtcNow
        ) 
            return new AuthenticatedUser
            {
                AuthenticationLevel = activeSession.AuthLevel,
                UserID = activeSession.UserID  
            };

        return null;
    }

    public bool RevokeSession (string? credential)
    {
        if(string.IsNullOrEmpty(credential)) return false;

        string hash = cryptographer.HashSessionCredential(credential);

        // Try to remove from memory
        sessionContexts.TryRemove(hash, out _);

        // Revoke in database
        return storageService.SetRevokeSessionFlag(hash);
    }

    internal SessionContext? GetSessionContext(string credential)
    {
        // Hash the credential the same way it was stored
        string hash = cryptographer.HashSessionCredential(credential);
        sessionContexts.TryGetValue(hash, out var context);
        return context;
    }

    // TODO CURRENTLY NO MFA SYSTEM IS SUPPORTED. UPGRADES ANY SESSION.
    public bool UpgradeSession(string credential, byte[] token)
    {
        // TODO IMPLEMENT TOKEN / MFA
        // if(token) ...

        AuthenticatedUser? authUser = ValidateSession(credential);
        if(authUser == null)
        {
            logger.LogWarning("Won't Upgrade session. No authenticated user.");
            return false;
        }

        String hash = cryptographer.HashSessionCredential(credential);
        if (String.IsNullOrEmpty(hash))
        {
            logger.LogError("Failed to create Hash! ");
            return false;
        }

        SessionCredential? session = storageService.GetSessionCredentialByCredentialHash(hash);
        if (session == null)
        {
            logger.LogError("Failed to retrieve session! ");
            return false;
        }

        SessionContext? context = GetSessionContext(credential);

        if(context != null)  // ← NULL CHECK!
        {
            context.AuthLevel = AuthenticationLevel.Full;
        }
        else
        {
            logger.LogWarning("SessionContext not found in RAM for credential: {hash}", hash);
            // TODO handle null
        }

        if(!storageService.UpgradeSession(session)) return false;

        logger.LogInformation("Successfully upgraded Session! ");
        return true;
    }

}