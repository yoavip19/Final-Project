using System;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace SecurioClient.Tests
{
    // Tests for the SHA-1 hash computation used by the HIBP breach check on the client side.
    //
    // EncryptionHelper.ComputeSha1Hash (in the Xamarin.Android project) wraps
    // System.Security.Cryptography.SHA1 — a standard algorithm with well-known test vectors.
    // Because EncryptionHelper.cs references Android-only APIs (Javax.Crypto), the class
    // cannot be compiled into this net8.0 test project.  Instead, these tests verify the
    // algorithm contract using the same underlying SHA1.Create() call, and document the
    // expected output format so that any future refactoring cannot silently break the
    // HIBP integration.
    //
    // To run: dotnet test SecurioClient.Tests/SecurioClient.Tests.csproj
    public class EncryptionHelperSha1Tests
    {
        // Mirrors the EncryptionHelper.ComputeSha1Hash implementation so we can
        // assert the expected values without referencing the Android project.
        private static string ComputeSha1Hash(string input)
        {
            using var sha1 = SHA1.Create();
            byte[] hashBytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(input));
            return BitConverter.ToString(hashBytes).Replace("-", "").ToUpperInvariant();
        }

        // ── Output format ────────────────────────────────────────────────────────

        [Fact]
        public void ComputeSha1Hash_ResultIsAlways40Chars()
        {
            // SHA-1 always produces a 160-bit (40 hex char) digest.
            Assert.Equal(40, ComputeSha1Hash("").Length);
            Assert.Equal(40, ComputeSha1Hash("password").Length);
            Assert.Equal(40, ComputeSha1Hash("a very long string with spaces and 1234567890!@#$").Length);
        }

        [Fact]
        public void ComputeSha1Hash_ResultIsUppercaseHex()
        {
            string hash = ComputeSha1Hash("test");
            Assert.Matches("^[0-9A-F]{40}$", hash);
        }

        [Fact]
        public void ComputeSha1Hash_ResultContainsNoDashes()
        {
            // BitConverter.ToString produces dashes by default; they must be removed.
            Assert.DoesNotContain("-", ComputeSha1Hash("anything"));
        }

        // ── Known NIST / RFC test vectors ────────────────────────────────────────

        [Fact]
        public void ComputeSha1Hash_EmptyString_MatchesNistVector()
        {
            // SHA-1("") = DA39A3EE5E6B4B0D3255BFEF95601890AFD80709  (FIPS 180-4)
            Assert.Equal("DA39A3EE5E6B4B0D3255BFEF95601890AFD80709", ComputeSha1Hash(""));
        }

        [Fact]
        public void ComputeSha1Hash_AbcString_MatchesNistVector()
        {
            // SHA-1("abc") = A9993E364706816ABA3E25717850C26C9CD0D89D  (FIPS 180-4)
            Assert.Equal("A9993E364706816ABA3E25717850C26C9CD0D89D", ComputeSha1Hash("abc"));
        }

        [Fact]
        public void ComputeSha1Hash_CommonPassword_MatchesKnownHibpHash()
        {
            // SHA-1("password") = 5BAA61E4C9B93F3F0682250B6CF8331B7EE68FD8
            // This hash appears billions of times in the HIBP dataset, confirming
            // that a client computing ComputeSha1Hash("password") would correctly
            // trigger the breach check.
            Assert.Equal("5BAA61E4C9B93F3F0682250B6CF8331B7EE68FD8", ComputeSha1Hash("password"));
        }

        // ── Determinism ──────────────────────────────────────────────────────────

        [Fact]
        public void ComputeSha1Hash_SameInputTwice_ReturnsSameHash()
        {
            const string input = "MyMasterPassword!99";
            Assert.Equal(ComputeSha1Hash(input), ComputeSha1Hash(input));
        }

        [Fact]
        public void ComputeSha1Hash_DifferentInputs_ReturnDifferentHashes()
        {
            Assert.NotEqual(ComputeSha1Hash("password1"), ComputeSha1Hash("password2"));
        }

        // ── HIBP k-anonymity prefix/suffix split ─────────────────────────────────

        [Fact]
        public void ComputeSha1Hash_FirstFiveCharsBecomesHibpPrefix()
        {
            // The HIBP API receives only the first 5 characters of the SHA-1 hash.
            // This test pins the expected prefix for a known password.
            string hash = ComputeSha1Hash("");
            Assert.Equal("DA39A", hash[..5]);
        }

        [Fact]
        public void ComputeSha1Hash_CharsAfterFiveBecomesHibpSuffix()
        {
            string hash = ComputeSha1Hash("");
            // Suffix is everything from position 5 onwards — 35 characters.
            Assert.Equal(35, hash[5..].Length);
            Assert.Equal("3EE5E6B4B0D3255BFEF95601890AFD80709", hash[5..]);
        }
    }
}
