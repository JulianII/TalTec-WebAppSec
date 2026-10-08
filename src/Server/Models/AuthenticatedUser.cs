 /*
  * This model is used to identify and authorize a user.
  */

public class AuthenticatedUser
{
    public int UserID { get; set; }                                 // Unique identifier
    public AuthenticationLevel AuthenticationLevel { get; set; }    // Describes the user's authentication level
}