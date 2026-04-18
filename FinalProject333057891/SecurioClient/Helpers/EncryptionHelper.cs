using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Java.Util;
using static Android.Provider.Settings;

namespace SecurioClient
{
    // A utility class used to generate random salts, transform passwords into secure keys, and encrypt vault data using AES-GCM.
    public static class EncryptionHelper
    {
        // High iterations make brute-force attacks much harder
        private const int Iterations = 50; ///600000;

        // Creates a cryptographically strong 32-byte random salt encoded as a Base64 string.
        public static string GenerateSalt()
        {
            byte[] saltBytes = new byte[32];
            RandomNumberGenerator.Fill(saltBytes);
            return Convert.ToBase64String(saltBytes);
        }

        // Transforms a plaintext password and salt into a secure 256-bit key through multiple hashing iterations.
        public static string DeriveKey(string password, string saltBase64)
        {
            byte[] saltBytes = Convert.FromBase64String(saltBase64);

            using (var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, Iterations, HashAlgorithmName.SHA256))
            {
                byte[] key = pbkdf2.GetBytes(32); // 256-bit key
                return Convert.ToBase64String(key);
            }
        }

        // Encrypts plaintext using AES-GCM with the given Base64-encoded 256-bit key.
        // Returns the IV, authentication Tag, and CipherText as Base64 strings.
        public static (string IV, string Tag, string CipherText) EncryptAesGcm(string plaintext, string base64Key)
        {
            byte[] keyBytes = Convert.FromBase64String(base64Key);

            // Generate a random 96-bit IV (recommended size for GCM).
            byte[] ivBytes = new byte[12];
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(ivBytes);

            // Use Java's Cipher API which is fully supported on Android.
            var cipher = Javax.Crypto.Cipher.GetInstance("AES/GCM/NoPadding");
            var keySpec = new Javax.Crypto.Spec.SecretKeySpec(keyBytes, "AES");
            var gcmSpec = new Javax.Crypto.Spec.GCMParameterSpec(128, ivBytes); // 128-bit auth tag
            cipher.Init(Javax.Crypto.CipherMode.EncryptMode, keySpec, gcmSpec);

            byte[] plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
            byte[] encrypted = cipher.DoFinal(plaintextBytes);

            // Java AES-GCM appends the 16-byte authentication tag to the ciphertext.
            int tagLengthBytes = 16;
            byte[] cipherBytes = new byte[encrypted.Length - tagLengthBytes];
            byte[] tagBytes = new byte[tagLengthBytes];
            Array.Copy(encrypted, 0, cipherBytes, 0, cipherBytes.Length);
            Array.Copy(encrypted, cipherBytes.Length, tagBytes, 0, tagLengthBytes);

            return (
                Convert.ToBase64String(ivBytes),
                Convert.ToBase64String(tagBytes),
                Convert.ToBase64String(cipherBytes)
            );
        }

        // Computes an unsalted SHA-1 hash of the input (uppercase hex). Used for HIBP breach checking only.
        public static string ComputeSha1Hash(string input)
        {
            using (var sha1 = SHA1.Create())
            {
                byte[] hashBytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(input));
                return BitConverter.ToString(hashBytes).Replace("-", "").ToUpperInvariant();
            }
        }
    }
}