public class StorageService
{
    public User GetUserByUsername (string username)
    {
        User requestedUser = new User (); 

        // Find user in DB
        requestedUser.UserID = 1;
        requestedUser.Username = username;
        requestedUser.PasswordVerifier = "Password";

        return requestedUser;
    }

    public bool CreateSessionEntry (SessionCredential session)
    {
        return true;
    }

    public SessionCredential GetSessionCredentialByCredential (string credential)
    {
        return new SessionCredential();
    }

    public bool SetRevokeSessionFlag (string CredentialHash)
    {
        return true;
    }
}