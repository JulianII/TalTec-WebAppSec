public class Cryptographer
{
    public string CreateArgon2idVerifier (string Password)
    {
        return "Argon2ID-Verifier";
    }

    public bool VerifyArgon2idPassword(string password, string storedVerifier)
    {
        if (password == storedVerifier) return true;
        else return false;
    }

    public string GenerateSessionCredential ()
    {
        return "SessionCredential";
    }

    public string HashSessionCredential (string credential)
    {
        return credential;
    }
}