using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace SecurioClient.Tests
{
    // Unit tests for the SHA-1 hash computation used by the HIBP breach check on
    // the client side.  EncryptionHelper.ComputeSha1Hash wraps SHA1.Create() —
    // a standard algorithm with well-known NIST test vectors.
    //
    // Because the production EncryptionHelper.cs references Android-only Javax.Crypto
    // APIs, these tests use the test-project stub in Stubs/EncryptionHelper.cs, which
    // provides an identical ComputeSha1Hash implementation using .NET's SHA1.Create().
    // To run: dotnet test SecurioClient.Tests/SecurioClient.Tests.csproj
    public class EncryptionHelperSha1Tests
    {
        // ── Output format ────────────────────────────────────────────────────────

        [Fact]
        public void ComputeSha1Hash_ResultIsAlways40Chars()
        {
            Assert.Equal(40, SecurioClient.EncryptionHelper.ComputeSha1Hash("").Length);
            Assert.Equal(40, SecurioClient.EncryptionHelper.ComputeSha1Hash("password").Length);
            Assert.Equal(40, SecurioClient.EncryptionHelper.ComputeSha1Hash("a very long string with spaces 1234567890!@#$").Length);
        }

        [Fact]
        public void ComputeSha1Hash_ResultIsUppercaseHex()
        {
            string hash = SecurioClient.EncryptionHelper.ComputeSha1Hash("test");
            Assert.Matches("^[0-9A-F]{40}$", hash);
        }

        [Fact]
        public void ComputeSha1Hash_ResultContainsNoDashes()
        {
            // BitConverter.ToString inserts dashes by default; they must be removed.
            Assert.DoesNotContain("-", SecurioClient.EncryptionHelper.ComputeSha1Hash("anything"));
        }

        // ── Known NIST / RFC test vectors ────────────────────────────────────────

        [Fact]
        public void ComputeSha1Hash_EmptyString_MatchesNistVector()
        {
            // SHA-1("") = DA39A3EE5E6B4B0D3255BFEF95601890AFD80709  (FIPS 180-4)
            Assert.Equal(
                "DA39A3EE5E6B4B0D3255BFEF95601890AFD80709",
                SecurioClient.EncryptionHelper.ComputeSha1Hash(""));
        }

        [Fact]
        public void ComputeSha1Hash_AbcString_MatchesNistVector()
        {
            // SHA-1("abc") = A9993E364706816ABA3E25717850C26C9CD0D89D  (FIPS 180-4)
            Assert.Equal(
                "A9993E364706816ABA3E25717850C26C9CD0D89D",
                SecurioClient.EncryptionHelper.ComputeSha1Hash("abc"));
        }

        [Fact]
        public void ComputeSha1Hash_CommonPassword_MatchesKnownHibpHash()
        {
            // SHA-1("password") = 5BAA61E4C9B93F3F0682250B6CF8331B7EE68FD8
            // Appears billions of times in HIBP — a client computing this hash would
            // correctly trigger the breach check.
            Assert.Equal(
                "5BAA61E4C9B93F3F0682250B6CF8331B7EE68FD8",
                SecurioClient.EncryptionHelper.ComputeSha1Hash("password"));
        }

        // ── Determinism ──────────────────────────────────────────────────────────

        [Fact]
        public void ComputeSha1Hash_SameInputTwice_ReturnsSameHash()
        {
            const string input = "MyMasterPassword!99";
            Assert.Equal(
                SecurioClient.EncryptionHelper.ComputeSha1Hash(input),
                SecurioClient.EncryptionHelper.ComputeSha1Hash(input));
        }

        [Fact]
        public void ComputeSha1Hash_DifferentInputs_ReturnDifferentHashes()
        {
            Assert.NotEqual(
                SecurioClient.EncryptionHelper.ComputeSha1Hash("password1"),
                SecurioClient.EncryptionHelper.ComputeSha1Hash("password2"));
        }

        // ── HIBP k-anonymity prefix/suffix split ─────────────────────────────────

        [Fact]
        public void ComputeSha1Hash_FirstFiveCharsBecomesHibpPrefix()
        {
            // The HIBP API receives only the first 5 characters of the SHA-1 hash.
            string hash = SecurioClient.EncryptionHelper.ComputeSha1Hash("");
            Assert.Equal("DA39A", hash[..5]);
        }

        [Fact]
        public void ComputeSha1Hash_CharsAfterFiveBecomesHibpSuffix()
        {
            string hash = SecurioClient.EncryptionHelper.ComputeSha1Hash("");
            // Suffix is everything from position 5 onward — 35 characters.
            Assert.Equal(35, hash[5..].Length);
            Assert.Equal("3EE5E6B4B0D3255BFEF95601890AFD80709", hash[5..]);
        }

        // ── AES-GCM round-trip (also tests stub consistency) ─────────────────────

        [Fact]
        public void EncryptDecrypt_RoundTrip_ReturnsOriginalPlaintext()
        {
            // A 32-byte (256-bit) key — all zeros is valid for AES-256.
            string key       = Convert.ToBase64String(new byte[32]);
            string original  = "super-secret-password!123";

            var (iv, tag, cipherText) = SecurioClient.EncryptionHelper.EncryptAesGcm(original, key);
            string decrypted          = SecurioClient.EncryptionHelper.DecryptAesGcm(iv, tag, cipherText, key);

            Assert.Equal(original, decrypted);
        }

        [Fact]
        public void EncryptDecrypt_EmptyPlaintext_RoundTrips()
        {
            string key = Convert.ToBase64String(new byte[32]);
            var (iv, tag, ct) = SecurioClient.EncryptionHelper.EncryptAesGcm("", key);
            Assert.Equal("", SecurioClient.EncryptionHelper.DecryptAesGcm(iv, tag, ct, key));
        }

        [Fact]
        public void EncryptDecrypt_UnicodePlaintext_RoundTrips()
        {
            string key = Convert.ToBase64String(new byte[32]);
            const string original = "pässwørd ✓ 日本語";
            var (iv, tag, ct) = SecurioClient.EncryptionHelper.EncryptAesGcm(original, key);
            Assert.Equal(original, SecurioClient.EncryptionHelper.DecryptAesGcm(iv, tag, ct, key));
        }

        [Fact]
        public void EncryptAesGcm_TwoCallsSamePlaintext_DifferentIVs()
        {
            // Each encryption must use a fresh random IV to ensure semantic security.
            string key = Convert.ToBase64String(new byte[32]);
            var (iv1, _, _) = SecurioClient.EncryptionHelper.EncryptAesGcm("test", key);
            var (iv2, _, _) = SecurioClient.EncryptionHelper.EncryptAesGcm("test", key);
            Assert.NotEqual(iv1, iv2);
        }
    }
}
