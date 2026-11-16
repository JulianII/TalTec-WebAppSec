public class TrustedDeviceCredential
{
    public int DeviceID { get; set; }
    public int UserID { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public bool Revoked { get; set; }
}
