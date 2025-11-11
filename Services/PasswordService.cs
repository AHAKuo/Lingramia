using System;
using System.Security.Cryptography;
using System.Text;

namespace Lingramia.Services;

/// <summary>
/// Service for encrypting and decrypting passwords using AES encryption.
/// Uses an internal private key known only to the application.
/// </summary>
public static class PasswordService
{
    // Internal private key for encryption (not user-configurable)
    private static readonly string PrivateKey = "Lingramia_Internal_Password_Key_2024_Secure";

    /// <summary>
    /// Encrypts a password using AES encryption.
    /// </summary>
    /// <param name="plainPassword">The plain text password to encrypt</param>
    /// <returns>Base64 encoded encrypted password</returns>
    public static string EncryptPassword(string plainPassword)
    {
        if (string.IsNullOrEmpty(plainPassword))
            return string.Empty;

        try
        {
            using (var aes = Aes.Create())
            {
                aes.Key = DeriveKey(PrivateKey, aes.KeySize / 8);
                aes.IV = DeriveIV(PrivateKey, aes.BlockSize / 8);
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var encryptor = aes.CreateEncryptor())
                {
                    var plainBytes = Encoding.UTF8.GetBytes(plainPassword);
                    var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
                    return Convert.ToBase64String(encryptedBytes);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error encrypting password: {ex.Message}");
            return string.Empty;
        }
    }

    /// <summary>
    /// Decrypts an encrypted password.
    /// </summary>
    /// <param name="encryptedPassword">The base64 encoded encrypted password</param>
    /// <returns>The decrypted plain text password</returns>
    public static string DecryptPassword(string encryptedPassword)
    {
        if (string.IsNullOrEmpty(encryptedPassword))
            return string.Empty;

        try
        {
            using (var aes = Aes.Create())
            {
                aes.Key = DeriveKey(PrivateKey, aes.KeySize / 8);
                aes.IV = DeriveIV(PrivateKey, aes.BlockSize / 8);
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var decryptor = aes.CreateDecryptor())
                {
                    var encryptedBytes = Convert.FromBase64String(encryptedPassword);
                    var decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
                    return Encoding.UTF8.GetString(decryptedBytes);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error decrypting password: {ex.Message}");
            return string.Empty;
        }
    }

    /// <summary>
    /// Verifies if a plain password matches the encrypted password.
    /// </summary>
    /// <param name="plainPassword">The plain text password to verify</param>
    /// <param name="encryptedPassword">The encrypted password to compare against</param>
    /// <returns>True if passwords match, false otherwise</returns>
    public static bool VerifyPassword(string plainPassword, string encryptedPassword)
    {
        if (string.IsNullOrEmpty(plainPassword) || string.IsNullOrEmpty(encryptedPassword))
            return false;

        try
        {
            var decrypted = DecryptPassword(encryptedPassword);
            return plainPassword == decrypted;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Derives a key from the private key string.
    /// </summary>
    private static byte[] DeriveKey(string key, int keySize)
    {
        using (var sha256 = SHA256.Create())
        {
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(key));
            var result = new byte[keySize];
            Array.Copy(hash, result, Math.Min(hash.Length, keySize));
            return result;
        }
    }

    /// <summary>
    /// Derives an IV from the private key string.
    /// </summary>
    private static byte[] DeriveIV(string key, int ivSize)
    {
        using (var md5 = MD5.Create())
        {
            var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(key + "_IV"));
            var result = new byte[ivSize];
            Array.Copy(hash, result, Math.Min(hash.Length, ivSize));
            return result;
        }
    }
}

