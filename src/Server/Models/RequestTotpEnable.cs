 /*
  * Request to enable TOTP for a user.
  * Contains the code to verify and enable TOTP.
  */

public class RequestTotpEnable
{
    public required string Code { get; set; }    // TOTP code to verify
    public required string Secret { get; set; }  // TOTP secret to enable
}
