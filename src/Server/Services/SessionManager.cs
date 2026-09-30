public class SessionManager (
        StorageService storage,
        Cryptographer cryptographer,
        ILogger<SessionManager> logger
    )
{
    private readonly int SessionExpiryTimeMinutes = 5;

    // TODO: Implement propper ExpiresAt and Error-Hanlding for Database issues.
    public SessionResult? CreateSession (AuthenticatedUser user)
    {
        DateTime currentTime = DateTime.UtcNow;
        DateTime expiry = currentTime.AddMinutes(SessionExpiryTimeMinutes);

        String credential = cryptographer.GenerateSessionCredential();

        // Create Raw Session Credentials
        SessionCredential session = new()
        {
            CredentialHash = cryptographer.HashSessionCredential(credential),
            UserID = user.UserID,
            AuthLevel = user.AuthenticationLevel,
            CreatedAt = currentTime,
            ExpiresAt = expiry,
            Revoked = false
        };

        // Create Session Entry into Database
        if (!storage.CreateSessionEntry(session)) return null;  

        // Create Session Result object
        SessionResult result = new()
        {
            Credential = credential,
            ExpiresAt = expiry
        };

        return result;
    }

    // TODO: provably should use better comparison than string == string
    public AuthenticatedUser? ValidateSession (string credential)
    {
        // Transform raw credential
        String hash = cryptographer.HashSessionCredential(credential);

        // Search for active session.
        SessionCredential activeSession = storage.GetSessionCredentialByCredential (hash);
        if (activeSession == null) return null;

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
        if(credential == null) return false;
        return storage.SetRevokeSessionFlag(cryptographer.HashSessionCredential(credential));
    }
}