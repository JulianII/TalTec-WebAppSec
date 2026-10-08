 /*
  * Simple data type describing the user's authentication level.
  */

public enum AuthenticationLevel
{
    Reduced,    // The user may only add new entries or retrieve credentials
    Full        // The user has full access to the vault and may edit entries
}