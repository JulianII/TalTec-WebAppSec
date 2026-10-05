public class User
{
    public int UserID { get; set; }
    public string Username { get; set; }
    public byte[] PasswordVerifier { get; set; }
    public byte[] PasswordSalt { get; set; }
    public ICollection<VaultEntry> VaultEntries { get; set; } = new List<VaultEntry>();
    
}