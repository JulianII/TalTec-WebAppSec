 /*
  * Database model used to identify and authenticate a user.
  */

public class User
{
    public int UserID                { get; set; } // Unique identifier for each user
    public string Username           { get; set; } // Account identifier for the vault account
    public byte[] PasswordVerifier   { get; set; } // Value used to verify the user's password
    public byte[] PasswordSalt       { get; set; } // Salt used to generate the password verifier
    public byte[] VaultSalt          { get; set; } // Salt used to derive the vault key

    // EF Core navigation property representing the user's vault entries.
    public ICollection<VaultEntry> VaultEntries { get; set; } = new List<VaultEntry>();
}