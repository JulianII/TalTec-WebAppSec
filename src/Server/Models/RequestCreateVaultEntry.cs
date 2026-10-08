 /*
  * This model is used when the user wants to create a new vault entry.
  */

public class RequestCreateVaultEntry
{
    public string Title { get; set; }                  // Title given by the user to name the vault entry

    public string Website { get; set; }                // Website the credentials belong to

    public string ExternalUsername { get; set; }       // Username used to log in to the website

    public string GeneratedPassword { get; set; }     // Password used to log in to the external website
}