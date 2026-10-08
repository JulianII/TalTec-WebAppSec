 /*
  * Data sent from a simple login form.
  * Used to identify and authenticate the user.
  */

public class RequestLogin
{
    public string Username { get; set; }    // Identifies the user's account
    public string Password { get; set; }    // Authenticates the user's account
}