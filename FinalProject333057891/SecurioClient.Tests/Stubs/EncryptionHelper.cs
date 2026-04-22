// Pure .NET stub that replaces the Android-dependent EncryptionHelper.
// Implements the same public API using System.Security.Cryptography instead
// of Javax.Crypto, so it can be compiled and tested in a net8.0 test project.
using System;
using System.Security.Cryptography;
using System.Text;

namespace SecurioClient
{
    public static class EncryptionHelper
    {
        // Match the production iteration count.
        private const int Iterations = 50;

        // Creates a cryptographically strong 32-byte random salt encoded as a Base64 string.
        public static string GenerateSalt()
        {
            byte[] saltBytes = new byte[32];
            RandomNumberGenerator.Fill(saltBytes);
            return Convert.ToBase64String(saltBytes);
        }

        // Transforms a plaintext password and salt into a secure 256-bit key via PBKDF2-SHA256.
        public static string DeriveKey(string password, string saltBase64)
        {
            byte[] saltBytes = Convert.FromBase64String(saltBase64);
            using var pbkdf2 = new Rfc2898DeriveBytes(
                password, saltBytes, Iterations, HashAlgorithmName.SHA256);
            return Convert.ToBase64String(pbkdf2.GetBytes(32));
        }

        // Encrypts plaintext using .NET AesGcm (same AES-GCM algorithm as the Android version).
        // Returns IV, authentication Tag, and CipherText as Base64 strings.
        public static (string IV, string Tag, string CipherText) EncryptAesGcm(
            string plaintext, string base64Key)
        {
            byte[] keyBytes    = Convert.FromBase64String(base64Key);
            byte[] ivBytes     = new byte[12]; // 96-bit nonce
            byte[] plainBytes  = Encoding.UTF8.GetBytes(plaintext);
            byte[] cipherBytes = new byte[plainBytes.Length];
            byte[] tagBytes    = new byte[16]; // 128-bit authentication tag

            RandomNumberGenerator.Fill(ivBytes);

            using var aes = new AesGcm(keyBytes, 16);
            aes.Encrypt(ivBytes, plainBytes, cipherBytes, tagBytes);

            return (
                Convert.ToBase64String(ivBytes),
                Convert.ToBase64String(tagBytes),
                Convert.ToBase64String(cipherBytes)
            );
        }

        // Decrypts AES-GCM data using .NET AesGcm.
        // IV, Tag, and CipherText must be Base64 strings produced by EncryptAesGcm.
        public static string DecryptAesGcm(
            string base64IV, string base64Tag, string base64CipherText, string base64Key)
        {
            byte[] keyBytes    = Convert.FromBase64String(base64Key);
            byte[] ivBytes     = Convert.FromBase64String(base64IV);
            byte[] tagBytes    = Convert.FromBase64String(base64Tag);
            byte[] cipherBytes = Convert.FromBase64String(base64CipherText);
            byte[] plainBytes  = new byte[cipherBytes.Length];

            using var aes = new AesGcm(keyBytes, 16);
            aes.Decrypt(ivBytes, cipherBytes, tagBytes, plainBytes);

            return Encoding.UTF8.GetString(plainBytes);
        }

        // Computes an unsalted SHA-1 hash of the input (uppercase hex).
        // Used for HIBP k-anonymity breach checking.
        public static string ComputeSha1Hash(string input)
        {
            using var sha1 = SHA1.Create();
            byte[] hashBytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(input));
            return BitConverter.ToString(hashBytes).Replace("-", "").ToUpperInvariant();
        }
    }
}
