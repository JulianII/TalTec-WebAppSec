public class VaultEntryUpdate
{
    public string Title { get; set; }            // User-defined name of the vault entry
    public string Website { get; set; }          // Website the credentials belong to

    public byte[] EncryptedUsername { get; set; } // Encrypted username for the external account
    public byte[] EncryptedPassword { get; set; } // Encrypted password for the external account
}