public class AuthenticationService(
    Cryptographer cryptographer,
    StorageService storage,
    ILogger<AuthenticationService> logger
    )
{
    public AuthenticatedUser? Login(LoginRequest request)
    {
        logger.LogInformation("Login Attempt for {username}", request.Username);

        // TEMPORARY TEST CASE FOR LOGIN WITHOUT DB BACKEND: DELETE AFTER, please
        if (request.Username == "test" && request.Password == "passwort")
        {
            return new AuthenticatedUser
            {
                UserID = 0,
                AuthenticationLevel = AuthenticationLevel.Reduced
            };
        }

        AuthenticatedUser authenticatedUser = new AuthenticatedUser();

        User databaseUser = storage.GetUserByUsername(request.Username);

        authenticatedUser.UserID = databaseUser.UserID;

        // Compare Password hashes
        if (cryptographer.VerifyArgon2idPassword(request.Password, databaseUser.PasswordVerifier)){
            authenticatedUser.AuthenticationLevel = AuthenticationLevel.Reduced;
            logger.LogInformation("Login Successfull for: {id}", authenticatedUser.UserID);
        } else {
            logger.LogWarning("Invalid Login for: {id}", authenticatedUser.UserID);
            return null;
        }

        return authenticatedUser;    
    }

}