using System.Security.Cryptography;
using Server.Data;

public class TrustedDeviceService(
    PasswordManagerDbContext db,
    Cryptographer cryptographer,
    ILogger<TrustedDeviceService> logger)
{
    public const string CookieName = "trusted-device";
    public const int CookieLifetimeDays = 365;

    public string? Create(int userId)
    {
        int activeDeviceCount = db.TrustedDevices.Count(
            device => device.UserID == userId &&
                      !device.Revoked &&
                      device.ExpiresAt > DateTime.UtcNow);
        if (activeDeviceCount >= 10)
        {
            return null;
        }

        string token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        DateTime createdAt = DateTime.UtcNow;
        db.TrustedDevices.Add(new TrustedDeviceCredential
        {
            UserID = userId,
            TokenHash = cryptographer.HashSessionCredential(token),
            CreatedAt = createdAt,
            ExpiresAt = createdAt.AddDays(CookieLifetimeDays),
            Revoked = false
        });
        db.SaveChanges();

        logger.LogInformation("Trusted device registered for user {UserId}", userId);
        return token;
    }

    public bool IsTrusted(int userId, string? token, bool updateLastUsed = false)
    {
        if (string.IsNullOrEmpty(token))
        {
            return false;
        }

        string tokenHash = cryptographer.HashSessionCredential(token);
        TrustedDeviceCredential? device = db.TrustedDevices.SingleOrDefault(
            item => item.UserID == userId &&
                    item.TokenHash == tokenHash &&
                    !item.Revoked &&
                    item.ExpiresAt > DateTime.UtcNow);
        if (device == null)
        {
            return false;
        }

        if (updateLastUsed)
        {
            device.LastUsedAt = DateTime.UtcNow;
            db.SaveChanges();
        }
        return true;
    }

    public IReadOnlyList<TrustedDeviceInfo> GetActiveDevices(int userId) =>
        db.TrustedDevices
            .Where(device => device.UserID == userId &&
                             !device.Revoked &&
                             device.ExpiresAt > DateTime.UtcNow)
            .OrderBy(device => device.CreatedAt)
            .Select(device => new TrustedDeviceInfo(
                device.DeviceID,
                device.CreatedAt,
                device.LastUsedAt))
            .ToList();

    public string? Revoke(int userId, int deviceId)
    {
        TrustedDeviceCredential? device = db.TrustedDevices.SingleOrDefault(
            item => item.DeviceID == deviceId &&
                    item.UserID == userId &&
                    !item.Revoked &&
                    item.ExpiresAt > DateTime.UtcNow);
        if (device == null)
        {
            return null;
        }

        device.Revoked = true;
        db.SaveChanges();
        logger.LogInformation(
            "Trusted device {DeviceId} revoked for user {UserId}",
            deviceId,
            userId);
        return device.TokenHash;
    }
}
