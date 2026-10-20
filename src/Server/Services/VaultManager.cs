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
        if (context == null)
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

    public VaultEntryUnencrypted? GetVaultEntry (int entryId, AuthenticatedUser user, SessionContext context)
    {
        if(!CheckAuthenticationLevel(user, AuthenticationLevel.Full)) return null;

        VaultEntryUnencrypted entryUnencrypted = new VaultEntryUnencrypted();

        // Get whole Vault Entry
        VaultEntry? entry = storageService.GetVaultEntry(entryId);
        if(entry == null)
        {
            logger.LogError("Failed to get vault entry. Entry doesn't exist. Entry ID {ID}", entryId);
            return null;
        }

        if(CheckUserIDs(entryId, user.UserID)) return null;

        string? plainPassword = cryptographer.Decrypt(entry.EncryptedUsername, context.VaultKey);
        if (String.IsNullOrEmpty(plainPassword))
        {
            logger.LogError("Unable to decrypt Password for UserID: {user} EntryID:{entry}", user.UserID, entryId);    
            return null;
        }
        entryUnencrypted.Password = plainPassword;

        string? plainUsername = cryptographer.Decrypt(entry.EncryptedUsername, context.VaultKey);
        if (String.IsNullOrEmpty(plainUsername))
        {
            logger.LogError("Unable to decrypt Username for UserID: {user} EntryID:{entry}", user.UserID, entryId);    
            return null;
        }
        entryUnencrypted.Username = plainUsername;

        return entryUnencrypted;
    }

    public bool EditVaultEntry (AuthenticatedUser user, VaultEntryUpdate entryUpdate, int entryId)
    {
        if(!CheckAuthenticationLevel(user, AuthenticationLevel.Full)) return false;
        if(!CheckUserIDs(entryId, user.UserID)) return false;

        VaultEntry? entry = storageService.GetVaultEntry(entryId);
        if (entry == null)
        {
            logger.LogError("Failed to edit vault entry. Entry doesn't exist. Entry ID {ID}", entryId);
            return false;
        }

        storageService.UpdateVaultEntry(entryUpdate, entryId);

        logger.LogInformation("Vault Entry {entry} Updated by: {user}", entryId, user.UserID);
        return true;
    }

    public bool DeleteVaulEntry (AuthenticatedUser user, int entryId)
    {
        if(!CheckAuthenticationLevel(user, AuthenticationLevel.Full)) return false;
        if(!CheckUserIDs(entryId, user.UserID)) return false;

        if (storageService.DeleteVaultEntry(entryId))
        {
            logger.LogError("Failed to delete entry: {entry}", entryId);
            return false;
        }

        return true;
    }    

    private bool CheckAuthenticationLevel (AuthenticatedUser user, AuthenticationLevel requiredLevel)
    {
        if(user.AuthenticationLevel == requiredLevel) return true;

        logger.LogWarning("Authentication level is insufficient. ");
        return false;
    }

    private bool CheckUserIDs(int entryId, int userId)
    {
        if(entryId == userId) return true;
        
        logger.LogWarning("UserIDs don't match! ID: {id} tried. ", userId);
        return false;
        
    }
}