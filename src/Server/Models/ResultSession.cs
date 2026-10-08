 /*
  * This model contains the session data sent to the user.
  */

public class ResultSession
{
    public string Credential { get; set; }      // Used to identify the session
    public DateTime ExpiresAt { get; set; }     // Indicates when the session expires; the server does not trust client-provided values
}