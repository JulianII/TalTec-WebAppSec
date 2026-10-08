public class AuthenticationService(
    Cryptographer cryptographer,
    StorageService storage,
    ILogger<AuthenticationService> logger
    )
{
    public AuthenticatedUser? Login(RequestLogin request)
    {
        logger.LogInformation("Login Attempt for {username}", request.Username);

        AuthenticatedUser authenticatedUser = new AuthenticatedUser();

        User? databaseUser = storage.GetUser(request.Username);
        if(databaseUser == null)
        {
            logger.LogWarning("User: {username} not found. ", request.Username);
            return null;
        }

        authenticatedUser.UserID = databaseUser.UserID;

        // Compare Password hashes
        if (cryptographer.VerifyArgon2idPassword(request.Password, databaseUser.PasswordSalt, databaseUser.PasswordVerifier)){
            authenticatedUser.AuthenticationLevel = AuthenticationLevel.Reduced;
            logger.LogInformation("Login Successfull for user: {username}", request.Username);
        } else {
            logger.LogWarning("Invalid Login for user: {username}", request.Username);
            return null;
        }

        return authenticatedUser;    
    }

}