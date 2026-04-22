using System;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace SecurioClient.Tests
{
    // Unit tests for the EncryptionHelper stub (which mirrors the Android production implementation).
    // Covers: SHA-1 hashing, salt generation, key derivation, and AES-GCM roundtrip.
    // All tests run entirely in-process — no Android SDK or network needed.
    public class EncryptionHelperTests
    {
        // ── ComputeSha1Hash: output format ───────────────────────────────────────

        [Fact]
        public void ComputeSha1Hash_ResultIsAlways40Chars()
        {
            Assert.Equal(40, EncryptionHelper.ComputeSha1Hash("").Length);
            Assert.Equal(40, EncryptionHelper.ComputeSha1Hash("password").Length);
            Assert.Equal(40, EncryptionHelper.ComputeSha1Hash("a very long string 12345!@#$").Length);
        }

        [Fact]
        public void ComputeSha1Hash_ResultIsUppercaseHex()
        {
            Assert.Matches("^[0-9A-F]{40}$", EncryptionHelper.ComputeSha1Hash("test"));
        }

        [Fact]
        public void ComputeSha1Hash_ResultContainsNoDashes()
        {
            Assert.DoesNotContain("-", EncryptionHelper.ComputeSha1Hash("anything"));
        }

        // ── ComputeSha1Hash: NIST test vectors ───────────────────────────────────

        [Fact]
        public void ComputeSha1Hash_EmptyString_MatchesNistVector()
        {
            // FIPS 180-4: SHA-1("") = DA39A3EE5E6B4B0D3255BFEF95601890AFD80709
            Assert.Equal("DA39A3EE5E6B4B0D3255BFEF95601890AFD80709",
                EncryptionHelper.ComputeSha1Hash(""));
        }

        [Fact]
        public void ComputeSha1Hash_Abc_MatchesNistVector()
        {
            // FIPS 180-4: SHA-1("abc") = A9993E364706816ABA3E25717850C26C9CD0D89D
            Assert.Equal("A9993E364706816ABA3E25717850C26C9CD0D89D",
                EncryptionHelper.ComputeSha1Hash("abc"));
        }

        [Fact]
        public void ComputeSha1Hash_CommonPassword_MatchesHibpKnownHash()
        {
            // SHA-1("password") = 5BAA61E4C9B93F3F0682250B6CF8331B7EE68FD8
            // This hash appears billions of times in the HIBP dataset.
            Assert.Equal("5BAA61E4C9B93F3F0682250B6CF8331B7EE68FD8",
                EncryptionHelper.ComputeSha1Hash("password"));
        }

        // ── ComputeSha1Hash: determinism and uniqueness ──────────────────────────

        [Fact]
        public void ComputeSha1Hash_SameInputTwice_ReturnsSameHash()
        {
            const string input = "MyMasterPassword!99";
            Assert.Equal(EncryptionHelper.ComputeSha1Hash(input),
                         EncryptionHelper.ComputeSha1Hash(input));
        }

        [Fact]
        public void ComputeSha1Hash_DifferentInputs_ReturnDifferentHashes()
        {
            Assert.NotEqual(EncryptionHelper.ComputeSha1Hash("password1"),
                            EncryptionHelper.ComputeSha1Hash("password2"));
        }

        // ── ComputeSha1Hash: HIBP prefix/suffix protocol ─────────────────────────

        [Fact]
        public void ComputeSha1Hash_FirstFiveChars_IsHibpPrefix()
        {
            Assert.Equal("DA39A", EncryptionHelper.ComputeSha1Hash("")[..5]);
        }

        [Fact]
        public void ComputeSha1Hash_SuffixIsRemaining35Chars()
        {
            Assert.Equal(35, EncryptionHelper.ComputeSha1Hash("")[5..].Length);
        }

        // ── GenerateSalt ─────────────────────────────────────────────────────────

        [Fact]
        public void GenerateSalt_ReturnsNonEmptyString()
        {
            Assert.False(string.IsNullOrWhiteSpace(EncryptionHelper.GenerateSalt()));
        }

        [Fact]
        public void GenerateSalt_IsValidBase64()
        {
            // Must not throw.
            byte[] bytes = Convert.FromBase64String(EncryptionHelper.GenerateSalt());
            Assert.Equal(32, bytes.Length);
        }

        [Fact]
        public void GenerateSalt_TwoCallsReturnDifferentValues()
        {
            // Probability of collision on 32 random bytes is negligible.
            Assert.NotEqual(EncryptionHelper.GenerateSalt(), EncryptionHelper.GenerateSalt());
        }

        [Fact]
        public void GenerateSalt_Produces32RandomBytes()
        {
            byte[] bytes = Convert.FromBase64String(EncryptionHelper.GenerateSalt());
            Assert.Equal(32, bytes.Length);
        }

        // ── DeriveKey ────────────────────────────────────────────────────────────

        [Fact]
        public void DeriveKey_ReturnsNonEmptyString()
        {
            string salt = EncryptionHelper.GenerateSalt();
            Assert.False(string.IsNullOrWhiteSpace(EncryptionHelper.DeriveKey("password", salt)));
        }

        [Fact]
        public void DeriveKey_ResultIsValidBase64_Producing32Bytes()
        {
            string salt = EncryptionHelper.GenerateSalt();
            byte[] key = Convert.FromBase64String(EncryptionHelper.DeriveKey("password", salt));
            Assert.Equal(32, key.Length); // 256-bit key
        }

        [Fact]
        public void DeriveKey_SamePasswordAndSalt_ReturnsSameKey()
        {
            string salt = EncryptionHelper.GenerateSalt();
            Assert.Equal(
                EncryptionHelper.DeriveKey("mypassword", salt),
                EncryptionHelper.DeriveKey("mypassword", salt));
        }

        [Fact]
        public void DeriveKey_DifferentPasswords_ReturnDifferentKeys()
        {
            string salt = EncryptionHelper.GenerateSalt();
            Assert.NotEqual(
                EncryptionHelper.DeriveKey("password1", salt),
                EncryptionHelper.DeriveKey("password2", salt));
        }

        [Fact]
        public void DeriveKey_SamePasswordDifferentSalts_ReturnDifferentKeys()
        {
            string salt1 = EncryptionHelper.GenerateSalt();
            string salt2 = EncryptionHelper.GenerateSalt();
            Assert.NotEqual(
                EncryptionHelper.DeriveKey("password", salt1),
                EncryptionHelper.DeriveKey("password", salt2));
        }

        // ── EncryptAesGcm / DecryptAesGcm: roundtrip ────────────────────────────

        private static string GenerateKey()
        {
            byte[] key = new byte[32];
            RandomNumberGenerator.Fill(key);
            return Convert.ToBase64String(key);
        }

        [Fact]
        public void EncryptDecrypt_ShortPlaintext_RoundtripSucceeds()
        {
            const string plaintext = "hello";
            string key = GenerateKey();
            var (iv, tag, ct) = EncryptionHelper.EncryptAesGcm(plaintext, key);
            Assert.Equal(plaintext, EncryptionHelper.DecryptAesGcm(iv, tag, ct, key));
        }

        [Fact]
        public void EncryptDecrypt_LongPlaintext_RoundtripSucceeds()
        {
            string plaintext = new string('x', 1000);
            string key = GenerateKey();
            var (iv, tag, ct) = EncryptionHelper.EncryptAesGcm(plaintext, key);
            Assert.Equal(plaintext, EncryptionHelper.DecryptAesGcm(iv, tag, ct, key));
        }

        [Fact]
        public void EncryptDecrypt_UnicodeChars_RoundtripSucceeds()
        {
            const string plaintext = "Ünïcödé pässwörð 🔐";
            string key = GenerateKey();
            var (iv, tag, ct) = EncryptionHelper.EncryptAesGcm(plaintext, key);
            Assert.Equal(plaintext, EncryptionHelper.DecryptAesGcm(iv, tag, ct, key));
        }

        [Fact]
        public void EncryptDecrypt_SpecialCharPassword_RoundtripSucceeds()
        {
            const string plaintext = "P@$$w0rd!#&*()";
            string key = GenerateKey();
            var (iv, tag, ct) = EncryptionHelper.EncryptAesGcm(plaintext, key);
            Assert.Equal(plaintext, EncryptionHelper.DecryptAesGcm(iv, tag, ct, key));
        }

        [Fact]
        public void Encrypt_TwiceWithSameKey_ProducesDifferentIVs()
        {
            string key = GenerateKey();
            var (iv1, _, _) = EncryptionHelper.EncryptAesGcm("test", key);
            var (iv2, _, _) = EncryptionHelper.EncryptAesGcm("test", key);
            Assert.NotEqual(iv1, iv2);
        }

        [Fact]
        public void Encrypt_TwiceWithSameKey_ProducesDifferentCiphertexts()
        {
            string key = GenerateKey();
            var (_, _, ct1) = EncryptionHelper.EncryptAesGcm("test", key);
            var (_, _, ct2) = EncryptionHelper.EncryptAesGcm("test", key);
            Assert.NotEqual(ct1, ct2);
        }

        [Fact]
        public void Decrypt_WrongKey_ThrowsAuthenticationTagMismatch()
        {
            string key1 = GenerateKey();
            string key2 = GenerateKey();
            var (iv, tag, ct) = EncryptionHelper.EncryptAesGcm("secret", key1);
            Assert.ThrowsAny<Exception>(() => EncryptionHelper.DecryptAesGcm(iv, tag, ct, key2));
        }

        [Fact]
        public void Decrypt_TamperedCiphertext_Throws()
        {
            string key = GenerateKey();
            var (iv, tag, ct) = EncryptionHelper.EncryptAesGcm("secret", key);
            byte[] ctBytes = Convert.FromBase64String(ct);
            ctBytes[0] ^= 0xFF; // flip bits in first byte
            string tamperedCt = Convert.ToBase64String(ctBytes);
            Assert.ThrowsAny<Exception>(() => EncryptionHelper.DecryptAesGcm(iv, tag, tamperedCt, key));
        }

        // ── DeriveKey + AES-GCM: full pipeline ───────────────────────────────────

        [Fact]
        public void DeriveKeyThenEncryptDecrypt_RoundtripSucceeds()
        {
            const string plaintext = "Str0ng!P@ssw0rd#99";
            const string masterPassword = "MasterPassword1!";
            string salt = EncryptionHelper.GenerateSalt();
            string vaultKey = EncryptionHelper.DeriveKey(masterPassword, salt);

            var (iv, tag, ct) = EncryptionHelper.EncryptAesGcm(plaintext, vaultKey);
            string decrypted = EncryptionHelper.DecryptAesGcm(iv, tag, ct, vaultKey);

            Assert.Equal(plaintext, decrypted);
        }

        [Fact]
        public void DeriveKey_DifferentSalt_ProducesDifferentVaultKey_CannotDecryptCrossKey()
        {
            const string password = "SameMasterPassword1!";
            const string plaintext = "vault-secret";
            string salt1 = EncryptionHelper.GenerateSalt();
            string salt2 = EncryptionHelper.GenerateSalt();

            string key1 = EncryptionHelper.DeriveKey(password, salt1);
            string key2 = EncryptionHelper.DeriveKey(password, salt2);

            var (iv, tag, ct) = EncryptionHelper.EncryptAesGcm(plaintext, key1);
            Assert.ThrowsAny<Exception>(() => EncryptionHelper.DecryptAesGcm(iv, tag, ct, key2));
        }
    }
}
