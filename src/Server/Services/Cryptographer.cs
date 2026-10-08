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

public class Cryptographer (ILogger<Cryptographer> logger)
{

    // Argoni2d configurations
    private const int SaltSize = 16; // 128 bits
    private const int HashSize = 32; // 256 bits
    private const int DegreeOfParallelism = 1; // Number of threads to use
    private const int Iterations = 2;
    private const int MemorySize = 19 * 1024; // 1 MiB

    // Settings for En and Decryption (AES 256 GCM)
    private const int nonceLength = 12;
    private const int tagLength = 16;
    private const int keyLength = 32;

    public  byte[] Argon2idHash(string password, byte[] salt)
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

    public ResultPasswordVerifier CreateArgon2idVerifier (string password)
    {
        
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] verifier = Argon2idHash(password, salt);

        ResultPasswordVerifier result = new ResultPasswordVerifier
        {
            PasswordVerifier = verifier,
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


    // The nonce and authentication tag are stored together with the ciphertext.
    // Their fixed lengths allow us to split the encrypted data during decryption.
    // [ Nonce (12) | Ciphertext (variable) | Tag (16) ]
    public byte[]? EncryptMessage(string message, byte[] key)
    {
        if (key.Length != keyLength)
        {
            logger.LogWarning("Keylength does not match for encryption.");
            return null;
        }

        if (string.IsNullOrEmpty(message))
        {
            logger.LogWarning("No Message to encrypt. ");
            return null;
        }

        byte[] pt = Encoding.UTF8.GetBytes(message);
        byte[] output = new byte[nonceLength + pt.Length + tagLength];
        
        Span<byte> nonce = output.AsSpan(0, nonceLength);
        Span<byte> ct = output.AsSpan(nonceLength, pt.Length);
        Span<byte> tag = output.AsSpan(nonceLength + pt.Length, tagLength);

        RandomNumberGenerator.Fill(nonce);

        using var aes = new AesGcm(key, tagLength);
        aes.Encrypt(nonce, pt, ct, tag);
        return output;
    }

    public string? Decrypt(byte[] data, byte[] key)
    {
        if(data.Length < (nonceLength + tagLength))
        {
            logger.LogError("Data length does not meet requirements. ");
            return null;
        }

        if (key.Length != keyLength)
        {
            // TODO: Propper error handling
            logger.LogWarning("Keylength does not match for decryption.");
            return null;
        }

        ReadOnlySpan<byte> nonce = data.AsSpan(0, nonceLength);
        ReadOnlySpan<byte> ct = data.AsSpan(nonceLength, data.Length - nonceLength - tagLength);
        ReadOnlySpan<byte> tag = data.AsSpan(data.Length - tagLength);
        
        byte[] pt = new byte[ct.Length];
        using var aes = new AesGcm(key, tagLength);

        try
        {
            aes.Decrypt(nonce, ct, tag, pt);
        }
        catch (System.Exception e)
        {
            logger.LogError(e, "Decryption Failed");
            throw;
        }
        

        return Encoding.UTF8.GetString(pt);
    }

    public string GeneratePassword(int passwordLength)
    {
        string validChars = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*?_-";  

        char[] chars = new char[passwordLength];  
        for (int i = 0; i < passwordLength; i++)  
        {  
            chars[i] = validChars[RandomNumberGenerator.GetInt32(validChars.Length)];  
        }  

        return new string(chars);
    }
}