 /*
  * Data model used internally to handle password verifier creation.
  */

public class ResultPasswordVerifier
{
    public byte[] PasswordVerifier { get; set; }    // The derived password verifier
    public byte[] Salt { get; set; }                // Salt used for password hashing
}