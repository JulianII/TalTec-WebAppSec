public class StorageService
{

    private SessionCredential? testSession;
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
        testSession = session;
        return true;
    }

    public SessionCredential? GetSessionCredentialByCredential(string credentialHash)
    {
        if (testSession?.CredentialHash == credentialHash)
        {
            return testSession;
        }

        return null;
    }

    public bool SetRevokeSessionFlag(string credentialHash)
    {   
        if (testSession?.CredentialHash != credentialHash)
        {
            return false;
        }

        testSession.Revoked = true;
        return true;
    }
}