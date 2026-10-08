 /*
  * This model is persistently stored in the database.
  * It is used to verify and authenticate a session.
  */

public class SessionCredential
{
    public string CredentialHash            { get; set; } // Hash of the session token, used for authentication
    public int UserID                       { get; set; } // Unique user identifier
    public AuthenticationLevel AuthLevel    { get; set; } // Authentication level of the session
    public DateTime CreatedAt               { get; set; } // Time when the session was created
    public DateTime ExpiresAt               { get; set; } // Time when the session expires
    public bool Revoked                     { get; set; } // Indicates whether the session manually was revoked
}