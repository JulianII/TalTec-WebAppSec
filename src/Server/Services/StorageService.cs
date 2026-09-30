using Server.Data;

public class StorageService (PasswordManagerDbContext db,
                            ILogger<StorageService> logger)
{
    public User? GetUserByUsername (string username)
    {
        User? user;

        // Get user from DB by username
        try { user = db.Users.SingleOrDefault(a => a.Username == username); }
        catch (System.InvalidOperationException)
        {
            logger.LogError("Multiple entries found for username: {username}", username);
            throw;
        }

        if (user == null) logger.LogInformation("Unable to find username: {username}", username);

        return user;
    }

    public bool CreateUser (User user)
    {
        try { 
            db.Users.Add(user); 
            db.SaveChanges();
        }
        catch (System.Exception ex)
        {
            logger.LogWarning(ex, "Failed to Create User: {username}", user.Username);
            return false;
        }

        logger.LogInformation("User registered: {username} at {time}", user.Username, DateTime.UtcNow);
        return true;
    }

    public bool CreateSessionEntry (SessionCredential session)
    {
        try { 
            db.Sessions.Add(session); 
            db.SaveChanges();
        }
        catch (System.Exception ex)
        {
            logger.LogWarning("Failed to Create Session for ID: {id} - {type}: {message}",
                                session.UserID,
                                ex.GetType().Name,
                                ex.Message);
            return false;
        }

        logger.LogInformation("Session created: {userID} at {time} until {expires}", 
                                session.UserID, 
                                DateTime.UtcNow, 
                                session.ExpiresAt);

        return true;
    }

    public SessionCredential? GetSessionCredentialByCredentialHash(string credentialHash)
    {
        SessionCredential? credential;

        // Get user from DB by username
        try { credential = db.Sessions.SingleOrDefault(a => a.CredentialHash == credentialHash); }
        catch (System.InvalidOperationException)
        {
            logger.LogError("Can't get session: Too many sessions for single hash: {hash}", credentialHash);
            throw;
        }

        if (credential == null) logger.LogInformation("Can't get session: Failed to find credential for hash: {hash}", credentialHash);

        return credential;
    }

    public bool SetRevokeSessionFlag(string credentialHash)
    {
        SessionCredential? session;

        // Get user from DB by username
        try { session = db.Sessions.SingleOrDefault(a => a.CredentialHash == credentialHash); }
        catch (System.InvalidOperationException)
        {
            logger.LogError("Cant revoke session: Too many sessions for single hash: {hash}", credentialHash);
            throw;
        }
        if (session == null) 
        {
            logger.LogInformation("Cant revoke session: Failed to find session for hash: {hash}", credentialHash);
            return false;
        }

        session.Revoked = true;
        db.SaveChanges();
        logger.LogInformation("Session revoked for: {id} at {time}", session.UserID, DateTime.UtcNow);

        return true;
    }
}