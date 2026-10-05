public class EncryptionResult
{
    public byte[] Ciphertext { get; set; }
    public byte[] Nonce { get; set; }
    public byte[] Tag { get; set; }
}