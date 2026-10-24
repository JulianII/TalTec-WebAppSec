 /*
  * Response containing TOTP setup information.
  * Returns the secret and QR code URL for TOTP setup.
  */

public class ResponseTotpSetup
{
    public required string Secret { get; set; } = string.Empty;      // Base32 encoded secret
    public required string QrCodeUrl { get; set; } = string.Empty;  // URL for QR code generation
}
