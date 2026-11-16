using System.Security.Cryptography;
using OtpNet;

public class AuthenticationService(
    Cryptographer cryptographer,
    StorageService storageService,
    ILogger<AuthenticationService> logger
    )
{

    private const int PeriodSeconds = 30;
    private const int CodeDigits = 6;
    private const string Issuer = "TalTec-PasswordManager";


    public AuthenticatedUser? LoginUser(RequestLogin request)
    {
        logger.LogInformation("Login attempt");

        AuthenticatedUser authenticatedUser = new AuthenticatedUser();

        User? databaseUser = storageService.GetUser(request.Username);
        if(databaseUser == null)
        {
            logger.LogWarning("Login failed: account was not found");
            return null;
        }

        authenticatedUser.UserID = databaseUser.UserID;

        // Compare Password hashes
        if (cryptographer.VerifyArgon2idPassword(request.Password, databaseUser.PasswordSalt, databaseUser.PasswordVerifier)){
            authenticatedUser.AuthenticationLevel = AuthenticationLevel.Reduced;
            logger.LogInformation("Login succeeded for user {UserId}", databaseUser.UserID);
        } else {
            logger.LogWarning("Login failed: invalid credentials");
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
            logger.LogWarning("Database failed to create a user account");
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
            logger.LogError("Failed to authenticate a newly created user account");
            return null;
        }

        return authenticated;
    }

    public string GenerateSecret()
    {
        var key = KeyGeneration.GenerateRandomKey(20);
        return Base32Encoding.ToString(key);
    }

    public string GetCurrentCode(string secret)
    {
        var keyBytes = Base32Encoding.ToBytes(secret);
        var totp = new Totp(keyBytes, PeriodSeconds, OtpHashMode.Sha1, CodeDigits);
        return totp.ComputeTotp();
    }

    public bool VerifyCode(string secret, string userCode)
    {
        try
        {
            var keyBytes = Base32Encoding.ToBytes(secret);
            var totp = new Totp(keyBytes, PeriodSeconds, OtpHashMode.Sha1, CodeDigits);
            return totp.VerifyTotp(userCode, out _, new VerificationWindow(1, 1));
        }
        catch { return false; }
    }

    public string GetQrCodeUrl(string secret, string username)
    {
        return $"otpauth://totp/{Uri.EscapeDataString(Issuer)}:{Uri.EscapeDataString(username)}?" +
               $"secret={secret}&issuer={Uri.EscapeDataString(Issuer)}&algorithm=SHA1&digits={CodeDigits}&period={PeriodSeconds}";
    }

}