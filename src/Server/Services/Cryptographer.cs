/** Docs:
* https://en.wikipedia.org/wiki/Argon2
* https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html
* https://github.com/kmaragon/Konscious.Security.Cryptography
* https://mojoauth.com/security-guides/argon2-in-c#how-to-store-and-verify-an-argon2id-hash-in-net
*/

using System.Net;
using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;

public class Cryptographer
{
    private const int SaltSize = 16; // 128 bits
    private const int HashSize = 32; // 256 bits
    private const int DegreeOfParallelism = 1; // Number of threads to use
    private const int Iterations = 2;
    private const int MemorySize = 19 * 1024; // 1 MiB

    static byte[] Argon2idHash(string password, byte[] salt)
    {
        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            DegreeOfParallelism = DegreeOfParallelism,
            MemorySize = MemorySize,   // KiB = 19 MiB
            Iterations = Iterations
        };
        return argon2.GetBytes(HashSize);
    }

    public PasswordVerifierResult CreateArgon2idVerifier (string password)
    {
        
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] verifier = Argon2idHash(password, salt);

        PasswordVerifierResult result = new PasswordVerifierResult
        {
            Hash = verifier,
            Salt = salt
        };

        return result;
    }

    public bool VerifyArgon2idPassword(string password, byte[] salt, byte[] storedVerifier)
    {
        byte[] actual = Argon2idHash(password, salt);
        return CryptographicOperations.FixedTimeEquals(actual, storedVerifier);
    }

    // TODO: THIS IS JUST FOR TESTING REMOVE AFTER DEV, please
    public string GenerateSessionCredential ()
    {
        // Generates a 32-byte (256-bit) array of cryptographically strong random values
        byte[] randomBytes = RandomNumberGenerator.GetBytes(32);
        string base64 = Convert.ToBase64String(randomBytes);
        return base64;
    }

    public string HashSessionCredential (string credential)
    {
        string hash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(credential))
        );
        
        return hash;
    }
}