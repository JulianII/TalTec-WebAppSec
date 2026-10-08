public class VaultManager (
    StorageService storageService,
    SessionManager sessionManager,
    Cryptographer cryptographer,
    ILogger<VaultManager> logger
)
{

    public bool CreateVaultEntry (string credentialHash, RequestCreateVaultEntry request)
    {
        // Verify user access.
        AuthenticatedUser? user = sessionManager.ValidateSession(credentialHash);
        if (user == null)
        {
            logger.LogWarning("Invalid session credential. Will not create Vault entry! ");
            return false;
        }

        logger.LogInformation ("Trying to create new Vault Entry for {ID} ", user.UserID);

        // Required fields validieren
        if (string.IsNullOrWhiteSpace(request.Website)           ||
            string.IsNullOrWhiteSpace(request.ExternalUsername)          ||
            string.IsNullOrWhiteSpace(request.MasterPassword)    ||
            string.IsNullOrWhiteSpace(request.GeneratedPassword) || 
            (request.Salt == null))
        {
            logger.LogInformation("Required fields not filled out.");
            return false;
        }

        // VaultEntry creation
        VaultEntry entry = new VaultEntry()
        {
            CreatedAt = DateTime.UtcNow,
            Website = request.Website,
            UserID = user.UserID
        };

        // Generate Password for credential
        string generatedPassword = cryptographer.GeneratePassword(32);

        byte[]? salt = storageService.GetVaultSalt(user.UserID);
        if(salt == null)
        {
            logger.LogWarning("Unable to find Vaultsalt for {ID} ", user.UserID);
            return false;    
        }

        byte[] key = cryptographer.Argon2idHash(request.MasterPassword, salt);
        byte[]? encryptedPassword = cryptographer.EncryptMessage(generatedPassword, key);
        if (encryptedPassword == null)
        {
            logger.LogError("Encrypted Password was not created. ");
            return false;
        }

        entry.EncryptedPassword = encryptedPassword;

        byte[]? encryptedUsername = cryptographer.EncryptMessage(request.ExternalUsername, key);
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