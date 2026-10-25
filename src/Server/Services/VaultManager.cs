public class VaultManager (
    StorageService storageService,
    SessionManager sessionManager,
    Cryptographer cryptographer,
    ILogger<VaultManager> logger
)
{

    public bool CreateVaultEntry (string sessionCredential, AuthenticatedUser userAuth, RequestCreateVaultEntry request)
    {
        if(!CheckAuthenticationLevel(userAuth, AuthenticationLevel.Reduced)) return false;

        logger.LogInformation ("Trying to create new Vault Entry for {ID} ", userAuth.UserID);

        // VaultEntry creation
        VaultEntry entry = new VaultEntry()
        {
            CreatedAt = DateTime.UtcNow,
            Website = request.Website,
            UserId = userAuth.UserID,
            Title = request.Title,
            UpdatedAt = DateTime.UtcNow
        };

        // Encrypt password using the VaultKey from the session context
        SessionContext? context = sessionManager.GetSessionContext(sessionCredential);
        if (context == null || !HasMatchingContext(userAuth, context))
        {
            logger.LogError("Unable to find Session Context for user: {user}", userAuth.UserID);
            return false;
        }

        byte[] vaultKey = context.VaultKey;


        byte[]? encryptedPassword = cryptographer.EncryptMessage(request.GeneratedPassword, vaultKey);
        if (encryptedPassword == null)
        {
            logger.LogError("Encrypted Password was not created. ");
            return false;
        }

        entry.EncryptedPassword = encryptedPassword;

        byte[]? encryptedUsername = cryptographer.EncryptMessage(request.ExternalUsername, vaultKey);
        if (encryptedUsername == null)
        {
            logger.LogError("Encrypted Username was not created. ");
            return false;
        }

        entry.EncryptedUsername = encryptedUsername;

        // VaultEntry über StorageService speichern
        if (!storageService.CreateVaultEntry(entry))
        {
            logger.LogError("Failed to create Vault Entry. ");
            return false;
        }

        return true;
    }

    public List<VaultEntryUnencrypted>? GetVaultEntries(AuthenticatedUser user, SessionContext context)
    {
        if (!CheckAuthenticationLevel(user, AuthenticationLevel.Full) || !HasMatchingContext(user, context))
            return null;

        List<VaultEntryUnencrypted> decryptedEntries = [];
        foreach (VaultEntry entry in storageService.GetVaultEntriesForUser(user.UserID))
        {
            VaultEntryUnencrypted? decryptedEntry = DecryptEntry(entry, context);
            if (decryptedEntry == null) return null;
            decryptedEntries.Add(decryptedEntry);
        }

        return decryptedEntries;
    }

    public VaultEntryUnencrypted? GetVaultEntry (int entryId, AuthenticatedUser user, SessionContext context)
    {
        if (!CheckAuthenticationLevel(user, AuthenticationLevel.Full) || !HasMatchingContext(user, context))
            return null;

        VaultEntry? entry = storageService.GetVaultEntryForUser(entryId, user.UserID);
        if(entry == null)
        {
            logger.LogError("Failed to get vault entry. Entry doesn't exist. Entry ID {ID}", entryId);
            return null;
        }

        return DecryptEntry(entry, context);
    }

    public bool EditVaultEntry(
        AuthenticatedUser user,
        SessionContext context,
        VaultEntryUpdate entryUpdate,
        int entryId)
    {
        if (!CheckAuthenticationLevel(user, AuthenticationLevel.Full) || !HasMatchingContext(user, context))
        {
            return false;
        }

        byte[]? encryptedPassword = cryptographer.EncryptMessage(entryUpdate.GeneratedPassword, context.VaultKey);
        byte[]? encryptedUsername = cryptographer.EncryptMessage(entryUpdate.ExternalUsername, context.VaultKey);
        if (encryptedPassword == null || encryptedUsername == null)
        {
            logger.LogWarning("Could not encrypt updated vault credentials for entry {EntryID}", entryId);
            return false;
        }

        if (!storageService.UpdateVaultEntry(
                entryId,
                user.UserID,
                entryUpdate.Title,
                entryUpdate.Website,
                encryptedUsername,
                encryptedPassword))
            return false;

        logger.LogInformation("Vault Entry {entry} Updated by: {user}", entryId, user.UserID);
        return true;
    }

    public bool DeleteVaultEntry (AuthenticatedUser user, int entryId)
    {
        if(!CheckAuthenticationLevel(user, AuthenticationLevel.Full)) return false;

        if (!storageService.DeleteVaultEntry(entryId, user.UserID))
        {
            logger.LogError("Failed to delete entry: {entry}", entryId);
            return false;
        }

        return true;
    }    

    private bool CheckAuthenticationLevel (AuthenticatedUser user, AuthenticationLevel requiredLevel)
    {
        if (user.AuthenticationLevel == AuthenticationLevel.Full ||
            (requiredLevel == AuthenticationLevel.Reduced &&
             user.AuthenticationLevel == AuthenticationLevel.Reduced))
            return true;

        logger.LogWarning("Authentication level is insufficient. ");
        return false;
    }

    private bool HasMatchingContext(AuthenticatedUser user, SessionContext context)
    {
        if (context.UserID == user.UserID) return true;

        logger.LogWarning("Session context user does not match authenticated user {UserID}", user.UserID);
        return false;
    }

    private VaultEntryUnencrypted? DecryptEntry(VaultEntry entry, SessionContext context)
    {
        string? username = cryptographer.Decrypt(entry.EncryptedUsername, context.VaultKey);
        string? password = cryptographer.Decrypt(entry.EncryptedPassword, context.VaultKey);
        if (username == null || password == null)
        {
            logger.LogError("Unable to decrypt credentials for vault entry {EntryID}", entry.EntryId);
            return null;
        }

        return new VaultEntryUnencrypted
        {
            EntryId = entry.EntryId,
            Title = entry.Title,
            Website = entry.Website,
            Username = username,
            Password = password
        };
    }
}