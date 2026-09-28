public class SessionCredential
{
    public string CredentialHash { get; set; }
    public int UserID { get; set; }
    public AuthenticationLevel AuthLevel { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool Revoked { get; set; }
}