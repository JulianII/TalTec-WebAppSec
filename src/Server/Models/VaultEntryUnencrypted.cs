 /*
  * This model is used when the user wants to create a new vault entry.
  */

public class VaultEntryUnencrypted
{
    public int EntryId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty; // Username used to log in to the website

    public string Password { get; set; } = string.Empty; // Password used to log in to the external website
}