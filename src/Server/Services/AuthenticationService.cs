using System.Security.Cryptography;

public class AuthenticationService(
    Cryptographer cryptographer,
    StorageService storageService,
    ILogger<AuthenticationService> logger
    )
{
    public AuthenticatedUser? LoginUser(RequestLogin request)
    {
        logger.LogInformation("Login Attempt for {username}", request.Username);

        AuthenticatedUser authenticatedUser = new AuthenticatedUser();

        User? databaseUser = storageService.GetUser(request.Username);
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

    public AuthenticatedUser? RegisterUser(RequestRegistration request)
    {
        ResultPasswordVerifier result = cryptographer.CreateArgon2idVerifier(request.Password);

        byte[] vaultSalt = RandomNumberGenerator.GetBytes(16);

        // create user for database
        User databaseEntry = new User
        {
            // UserID - left blank gets auto gened.
            PasswordVerifier = result.PasswordVerifier,
            Username = request.Username,
            PasswordSalt = result.Salt,
            VaultSalt = vaultSalt
        };

        if (!storageService.CreateUser(databaseEntry))
        {
            logger.LogWarning("Database failed to create user: {username}", request.Username);
            return null;
        }

        // TODO: Avoid recalculating Argon2id during auto-login after registration; reuse the authenticated user directly.
        // Create authenticatedUser for created User
        AuthenticatedUser? authenticated = LoginUser(new RequestLogin
        {
            Password = request.Password,
            Username = request.Username   
        });

        if (authenticated == null)
        {
            logger.LogError("Failed to authenticate just created user: {username}", request.Username);
            return null;
        }

        return authenticated;
    }

}