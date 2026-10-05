public class VaultEntry
{
    public int EntryID { get; set; }

    public int UserID { get; set; }

    public string Title { get; set; }
    public string Website { get; set; }

    public byte[] EncryptedUsername { get; set; }

    public byte[] EncryptedPassword { get; set; }

    public byte[] EncryptedNotes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
    public User User { get; set; }
}
