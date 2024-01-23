using System;
using System.Security.Cryptography;
using System.Text;

public class EncryptionHandler
{
    private static RSACryptoServiceProvider rsa;

    public static string Encrypt(string publicKey, string plainText)
    {
        rsa = new RSACryptoServiceProvider();
        rsa.FromXmlString(publicKey);

        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
        byte[] encryptedBytes = rsa.Encrypt(plainBytes, false);

        return Convert.ToBase64String(encryptedBytes);
    }

    public static string Decrypt(string privateKey, string encryptedText)
    {
        rsa = new RSACryptoServiceProvider();
        rsa.FromXmlString(privateKey);

        byte[] encryptedBytes = Convert.FromBase64String(encryptedText);
        byte[] decryptedBytes = rsa.Decrypt(encryptedBytes, false);

        return Encoding.UTF8.GetString(decryptedBytes);
    }
}
