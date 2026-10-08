/*
 * This model only exists in RAM and is not stored in the database.
 *
 * It is primarily used to store the VaultKey for an authenticated session.
 * By keeping the VaultKey separate from the database entities, we ensure
 * that the VaultKey is not permanently stored in the database.
 */
class SessionContext
{
    public string CredentialHash         { get; set; } // Hash of the session token, used to identify the session
    public int UserID                    { get; set; } // Unique user identifier
    public AuthenticationLevel AuthLevel { get; set; } // Authentication level of the session
    public byte[] VaultKey                { get; set; } // Key used to encrypt and decrypt vault data
}