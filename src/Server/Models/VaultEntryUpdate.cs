public class VaultEntryUpdate
{
    public string Title { get; set; } = string.Empty;   // User-defined name of the vault entry
    public string Website { get; set; } = string.Empty; // Website the credentials belong to

    // Plaintext request values are encrypted by the server before persistence.
    public string ExternalUsername { get; set; } = string.Empty;  // Username used to log in to the external account
    public string GeneratedPassword { get; set; } = string.Empty; // Password used to log in to the external account
}