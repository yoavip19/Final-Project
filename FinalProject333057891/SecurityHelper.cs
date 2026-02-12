using System;
using System.IO;
using System.Security.Cryptography;

namespace FinalProject333057891
{
    public static class SecurityHelper
    {
        private const int Iterations = 105_173;
        private const int SaltLength = 16;
        public static string GenerateSaltBase64()
        {
            byte[] salt = new byte[SaltLength];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            return Convert.ToBase64String(salt);
        }
        public static string HashPassword(string password, string saltBase64)
        {
            byte[] salt = Convert.FromBase64String(saltBase64);
            var deriveBytes = new Rfc2898DeriveBytes(password, salt, Iterations);
            return Convert.ToBase64String(deriveBytes.GetBytes(32)); // 256-bit output
        }

        // Encrypts data and outputs Base64 cipher, salt, and IV
        public static string EncryptAES(string dataToEncrypt, string? masterPassword, out string saltBase64, out string ivBase64)
        {
            if(masterPassword == null)
            {
                throw new ArgumentNullException(nameof(masterPassword), "Master password cannot be null.");
            }
            byte[] salt = new byte[SaltLength];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            byte[] iv;

            using var aes = Aes.Create();
            aes.KeySize = 256;

            using var keyDerivation = new Rfc2898DeriveBytes(masterPassword, salt, Iterations, HashAlgorithmName.SHA256);

            aes.Key = keyDerivation.GetBytes(32);
            aes.GenerateIV();
            iv = aes.IV;

            using var ms = new MemoryStream();

            // IMPORTANT: close writer before reading MemoryStream
            using (var encryptor = aes.CreateEncryptor())
            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            using (var sw = new StreamWriter(cs))
            {
                sw.Write(dataToEncrypt);
            } // ← FINAL BLOCK IS WRITTEN HERE

            byte[] cipherBytes = ms.ToArray();

            // Convert to Base64
            saltBase64 = Convert.ToBase64String(salt);
            ivBase64 = Convert.ToBase64String(iv);
            return Convert.ToBase64String(cipherBytes);
        }

        // Decrypts Base64 ciphertext using the given salt and IV (also Base64)
        public static string DecryptAES(string encryptedBase64, string? masterPassword, string saltBase64, string ivBase64)
        {
            if (masterPassword == null)
            {
                throw new ArgumentNullException(nameof(masterPassword), "Master password cannot be null.");
            }
            byte[] cipherBytes = Convert.FromBase64String(encryptedBase64);
            byte[] salt = Convert.FromBase64String(saltBase64);
            byte[] iv = Convert.FromBase64String(ivBase64);

            using var aes = Aes.Create();
            aes.KeySize = 256;

            using var keyDerivation = new Rfc2898DeriveBytes(masterPassword, salt, Iterations, HashAlgorithmName.SHA256);

            aes.Key = keyDerivation.GetBytes(32);
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            using var ms = new MemoryStream(cipherBytes);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs);

            return sr.ReadToEnd();
        }
    }
}