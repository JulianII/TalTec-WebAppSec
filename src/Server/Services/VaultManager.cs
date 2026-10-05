public class VaultManager (
    StorageService storageService,
    SessionManager sessionManager,
    Cryptographer cryptographer,
    ILogger<VaultManager> logger
)
{

    // The nonce and authentication tag are stored together with the ciphertext.
    // Their fixed lengths allow us to split the encrypted data during decryption.
    // [ Nonce (12) | Ciphertext (variable) | Tag (16) ]
    private const int nonceLength = 12;
    private const int tagLength = 16;

    public bool CreateVaultEntry (string credential, CreateVaultEntryRequest request)
    {
        // Verify user access.
        AuthenticatedUser? user = sessionManager.ValidateSession(credential);
        if (user == null)
        {
            logger.LogWarning("Invalid session credential.");
            return false;
        }

        logger.LogInformation ("Trying to create new Vault Entry for {ID} ", user.UserID);

        EncryptionResult EncryptedNotes = cryptographer.EncryptMessage(request.Notes);

        VaultEntry entry = new VaultEntry ()
        {
            CreatedAt = DateTime.UtcNow,
            EncryptedNotes = "",
        };

        return storageService.CreateVaultEntry(entry);
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