public class VaultManager (
    StorageService storageService,
    SessionManager sessionManager,
    Cryptographer cryptographer,
    ILogger<VaultManager> logger
)
{

    public bool CreateVaultEntry (string sessionCredential, RequestCreateVaultEntry request)
    {
        // Verify user access.
        AuthenticatedUser? userAuth = sessionManager.ValidateSession(sessionCredential);
        if (userAuth == null)
        {
            logger.LogWarning("Invalid session credential. Will not create Vault entry! ");
            return false;
        }

        logger.LogInformation ("Trying to create new Vault Entry for {ID} ", userAuth.UserID);

        // Required fields validieren
        if (string.IsNullOrWhiteSpace(request.Website)           ||
            string.IsNullOrWhiteSpace(request.ExternalUsername)  ||
            string.IsNullOrWhiteSpace(request.GeneratedPassword)   )
        {
            logger.LogInformation("Required fields not filled out.");
            return false;
        }

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

    public bool EditVaultEntry ()
    {
        return true;
    }

    public bool DeleteVaulEntry ()
    {
        return true;
    }    
}