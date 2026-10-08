 /*
  * Database model used to store a user's encrypted vault entry.
  */

public class VaultEntry
{
    public int EntryID { get; set; }             // Unique identifier for each vault entry

    public int UserID { get; set; }              // Identifier of the user who owns the entry

    public string Title { get; set; }            // User-defined name of the vault entry
    public string Website { get; set; }          // Website the credentials belong to

    public byte[] EncryptedUsername { get; set; } // Encrypted username for the external account
    public byte[] EncryptedPassword { get; set; } // Encrypted password for the external account

    public DateTime CreatedAt { get; set; }      // Time when the entry was created
    public DateTime UpdatedAt { get; set; }      // Time when the entry was last updated
}