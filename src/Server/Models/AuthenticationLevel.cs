 /*
  * Simple data type describing the user's authentication level.
  */

public enum AuthenticationLevel
{
    Reduced,    // The user may add new entries
    Full        // The user may add, retrieve, edit, and delete vault entries
}