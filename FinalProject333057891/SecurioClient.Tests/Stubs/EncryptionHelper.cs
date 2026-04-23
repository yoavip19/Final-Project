using System;
using System.Security.Cryptography;
using System.Text;

namespace SecurioClient
{
    // Test-only stub for EncryptionHelper using .NET's System.Security.Cryptography.AesGcm.
    // This replaces the Android Javax.Crypto version so that WarningsHelper and any other
    // helper compiled into the net8.0 test project can run without the Android SDK.
    //
    // The AES-GCM contract (IV/Tag/CipherText as Base64 strings) is identical to the
    // production Android implementation; tests that encrypt with this stub and then
    // decrypt should round-trip correctly.
    public static class EncryptionHelper
    {
        private const int Iterations = 50;

        // Encrypts plaintext using AES-GCM with a 32-byte key (Base64-encoded).
        // Returns IV, Tag, and CipherText as Base64 strings — same format as the
        // Android production version so WarningsHelper.GetWeakItems works in tests.
        public static (string IV, string Tag, string CipherText) EncryptAesGcm(
            string plaintext, string base64Key)
        {
            byte[] keyBytes       = Convert.FromBase64String(base64Key);
            byte[] ivBytes        = new byte[12];
            RandomNumberGenerator.Fill(ivBytes);

            byte[] plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
            byte[] cipherBytes    = new byte[plaintextBytes.Length];
            byte[] tagBytes       = new byte[16];

            using var aes = new AesGcm(keyBytes, 16);
            aes.Encrypt(ivBytes, plaintextBytes, cipherBytes, tagBytes);

            return (
                Convert.ToBase64String(ivBytes),
                Convert.ToBase64String(tagBytes),
                Convert.ToBase64String(cipherBytes)
            );
        }

        // Decrypts AES-GCM data using a 32-byte key (Base64-encoded).
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

        // Computes an unsalted SHA-1 hash of the input (uppercase hex). Used for HIBP breach checking.
        public static string ComputeSha1Hash(string input)
        {
            using var sha1  = SHA1.Create();
            byte[] hashBytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(input));
            return BitConverter.ToString(hashBytes).Replace("-", "").ToUpperInvariant();
        }

        // Generates a cryptographically random 32-byte salt as Base64.
        public static string GenerateSalt()
        {
            byte[] saltBytes = new byte[32];
            RandomNumberGenerator.Fill(saltBytes);
            return Convert.ToBase64String(saltBytes);
        }

        // Derives a 256-bit key from a password and a Base64-encoded salt using PBKDF2-SHA256.
        public static string DeriveKey(string password, string saltBase64)
        {
            byte[] saltBytes = Convert.FromBase64String(saltBase64);
            using var pbkdf2 = new Rfc2898DeriveBytes(
                password, saltBytes, Iterations, HashAlgorithmName.SHA256);
            return Convert.ToBase64String(pbkdf2.GetBytes(32));
        }
    }
}
