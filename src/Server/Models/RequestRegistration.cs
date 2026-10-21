 /*
  * Data sent from a simple registration form.
  * Used to create a new user.
  */

public class RequestRegistration
{
    public string Username { get; set; }        // Identifies the user's account
    public string Password { get; set; }        // Password used to authenticate the user's account
    public string VerifyPassword { get; set; }  // Used to verify the password entry
}