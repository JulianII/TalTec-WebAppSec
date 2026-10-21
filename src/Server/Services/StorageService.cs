using Server.Data;
using System.Collections.Concurrent;

public class StorageService (PasswordManagerDbContext db,
                            ILogger<StorageService> logger)
{
     private const int SessionExpiryTimeMinutes = 5;

    public bool CreateVaultEntry(VaultEntry entry)
    {
        try
    {
        db.VaultEntries.Add(entry); 
        db.SaveChanges();
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Failed to create VaultEntry for UserID: {UserId}", entry.UserId);
    return false;
    }

    logger.LogInformation("VaultEntry created for UserID: {UserId}", entry.UserId);
    // Log successful creation without logging credentials
    return true;
    }

    public User? GetUser (string username)
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

    public User? GetUser (int userID)
    {
        User? user;

        // Get user from DB by username
        try { user = db.Users.SingleOrDefault(a => a.UserID == userID); }
        catch (System.InvalidOperationException)
        {
            logger.LogError("Multiple entries found for UserID: {iD}", userID);
            throw;
        }

        if (user == null) logger.LogInformation("Unable to find UserID: {ID}", userID);

        return user;
    }

    public byte[]? GetVaultSalt(int userID)
    {
        User? user = db.Users.SingleOrDefault(a => a.UserID == userID);
        if (user == null) logger.LogInformation("Unable to find username: {ID}", userID);

        return user?.VaultSalt;
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
            logger.LogError("Can't get session: Too many sessions for single hash");
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

    public VaultEntry? GetVaultEntry (int entryId)
    {
        VaultEntry? entry = db.VaultEntries.SingleOrDefault(a => a.EntryId == entryId);
        if(entry == null) logger.LogError("Unable to retrieve EntryID: {ID}", entryId);

        return entry;
    }

    // TODO extensive logging.
    public bool UpdateVaultEntry (VaultEntryUpdate entryUpdate, int entryId)
    {
        VaultEntry? entry;

        // Get user from DB by username
        try { entry = db.VaultEntries.SingleOrDefault(a => a.EntryId == entryId); }
        catch (System.InvalidOperationException)
        {
            logger.LogError("Cant find entry: Too many entires for single ID: {ID}", entryId);
            throw;
        }

        if (entry == null) 
        {
            logger.LogInformation("Cant find entry: No entry found for ID: {ID}", entryId);
            return false;
        }

        entry.Title = entryUpdate.Title;
        entry.Website = entryUpdate.Website;
        entry.EncryptedPassword = entryUpdate.EncryptedPassword;
        entry.EncryptedUsername = entryUpdate.EncryptedUsername;
        entry.UpdatedAt = DateTime.UtcNow;

        db.SaveChanges();
        logger.LogInformation("EntryID {entry} Updated forUser: {id} at {time}", entryId, entry.EntryId, DateTime.UtcNow);

        return true;
    }

    public bool DeleteVaultEntry(int entryId)
    {
        try
        {
            VaultEntry? entry = db.VaultEntries.SingleOrDefault(e => e.EntryId == entryId);
            if (entry == null) return false;

            db.VaultEntries.Remove(entry);
            db.SaveChanges();
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to delete vault entry {ID}", entryId);
            return false;
        }
    }

    public bool UpgradeSession(SessionCredential session){
        try
        {
            session = db.Sessions.SingleOrDefault(a => a.CredentialHash == session.CredentialHash);
            if(session == null) return false;
            
            session.ExpiresAt = DateTime.UtcNow.AddMinutes(SessionExpiryTimeMinutes);
            session.AuthLevel = AuthenticationLevel.Full;

            db.SaveChanges();
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to Upgrade session for {ID}", session.UserID);
            return false;
        }
    }
}