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
    // A utility class used to generate random salts and transform passwords into secure keys using the PBKDF2 algorithm.
    public static class EncryptionHelper
    {
        // High iterations make brute-force attacks much harder
        private const int Iterations = 50; ///500000;

        // Creates a cryptographically strong 32-byte random salt encoded as a Base64 string.
        public static string GenerateSalt()
        {
            byte[] saltBytes = new byte[32];
            using (var provider = new RNGCryptoServiceProvider())
            {
                provider.GetBytes(saltBytes);
            }
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
    }
}