using SecurioClient.Helpers;
using Xunit;

namespace SecurioClient.Tests
{
    // Comprehensive unit tests for ValidationHelper — field validation and
    // password-strength utilities.  ValidationHelper is a pure .NET class compiled
    // directly into this test project (see SecurioClient.Tests.csproj).
    // To run: dotnet test SecurioClient.Tests/SecurioClient.Tests.csproj
    public class ValidationHelperTests
    {
        // ── ValidateUsername ─────────────────────────────────────────────────────

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ValidateUsername_NullOrEmpty_ReturnsFail(string? username)
        {
            var result = ValidationHelper.ValidateUsername(username!);
            Assert.False(result.IsValid);
            Assert.Equal("Username is required.", result.ErrorMessage);
        }

        [Theory]
        [InlineData("ab")]   // 2 chars — below minimum
        [InlineData("a")]    // 1 char
        public void ValidateUsername_TooShort_ReturnsFail(string username)
        {
            var result = ValidationHelper.ValidateUsername(username);
            Assert.False(result.IsValid);
            Assert.Equal("Username must be at least 3 characters.", result.ErrorMessage);
        }

        [Fact]
        public void ValidateUsername_TooLong_ReturnsFail()
        {
            // 31-character username exceeds the 30-char maximum.
            var result = ValidationHelper.ValidateUsername(new string('a', 31));
            Assert.False(result.IsValid);
            Assert.Equal("Username must be 30 characters or fewer.", result.ErrorMessage);
        }

        [Theory]
        [InlineData("1abc")]   // starts with digit
        [InlineData("_abc")]   // starts with underscore
        [InlineData("-abc")]   // starts with hyphen
        public void ValidateUsername_StartsWithNonLetter_ReturnsFail(string username)
        {
            var result = ValidationHelper.ValidateUsername(username);
            Assert.False(result.IsValid);
            Assert.Equal("Username must start with a letter.", result.ErrorMessage);
        }

        [Theory]
        [InlineData("abc!")]      // contains exclamation mark
        [InlineData("abc def")]   // contains space
        [InlineData("abc@123")]   // contains @
        public void ValidateUsername_InvalidChars_ReturnsFail(string username)
        {
            var result = ValidationHelper.ValidateUsername(username);
            Assert.False(result.IsValid);
            Assert.Equal("Username may only contain letters, digits, underscores, or hyphens.", result.ErrorMessage);
        }

        [Theory]
        [InlineData("abc")]                                             // exactly 3 chars — lower boundary
        [InlineData("Alice")]                                           // mixed case
        [InlineData("user_123")]                                        // letters + digits + underscore
        [InlineData("my-name")]                                         // letters + hyphen
        [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]                  // exactly 30 chars — upper boundary
        public void ValidateUsername_ValidInputs_ReturnsOk(string username)
        {
            var result = ValidationHelper.ValidateUsername(username);
            Assert.True(result.IsValid);
            Assert.Null(result.ErrorMessage);
        }

        // ── ValidateEmail ────────────────────────────────────────────────────────

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ValidateEmail_NullOrEmpty_ReturnsFail(string? email)
        {
            var result = ValidationHelper.ValidateEmail(email!);
            Assert.False(result.IsValid);
            Assert.Equal("Email address is required.", result.ErrorMessage);
        }

        [Theory]
        [InlineData("notanemail")]            // no @ symbol
        [InlineData("missing@domain")]        // no TLD
        [InlineData("@nodomain.com")]         // no local part
        [InlineData("two@@signs.com")]        // two @ symbols
        [InlineData("spaces in@email.com")]   // space in local part
        public void ValidateEmail_InvalidFormat_ReturnsFail(string email)
        {
            var result = ValidationHelper.ValidateEmail(email);
            Assert.False(result.IsValid);
            Assert.Equal("Please enter a valid email address.", result.ErrorMessage);
        }

        [Theory]
        [InlineData("user@example.com")]
        [InlineData("user.name+tag@sub.domain.org")]
        [InlineData("USER@EXAMPLE.COM")]
        [InlineData("user123@test.co.uk")]
        public void ValidateEmail_ValidFormats_ReturnsOk(string email)
        {
            var result = ValidationHelper.ValidateEmail(email);
            Assert.True(result.IsValid);
        }

        // ── ValidatePassword ─────────────────────────────────────────────────────

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void ValidatePassword_NullOrEmpty_ReturnsFail(string? password)
        {
            var result = ValidationHelper.ValidatePassword(password!);
            Assert.False(result.IsValid);
            Assert.Equal("Password is required.", result.ErrorMessage);
        }

        [Fact]
        public void ValidatePassword_TooShort_ReturnsFail()
        {
            var result = ValidationHelper.ValidatePassword("Ab1!567"); // 7 chars
            Assert.False(result.IsValid);
            Assert.Equal("Password must be at least 8 characters.", result.ErrorMessage);
        }

        [Fact]
        public void ValidatePassword_NoUppercase_ReturnsFail()
        {
            var result = ValidationHelper.ValidatePassword("abc12345!");
            Assert.False(result.IsValid);
            Assert.Equal("Password must include at least one uppercase letter.", result.ErrorMessage);
        }

        [Fact]
        public void ValidatePassword_NoLowercase_ReturnsFail()
        {
            var result = ValidationHelper.ValidatePassword("ABC12345!");
            Assert.False(result.IsValid);
            Assert.Equal("Password must include at least one lowercase letter.", result.ErrorMessage);
        }

        [Fact]
        public void ValidatePassword_NoDigit_ReturnsFail()
        {
            var result = ValidationHelper.ValidatePassword("Abcdefgh!");
            Assert.False(result.IsValid);
            Assert.Equal("Password must include at least one digit.", result.ErrorMessage);
        }

        [Fact]
        public void ValidatePassword_NoSpecialChar_ReturnsFail()
        {
            var result = ValidationHelper.ValidatePassword("Abcdefg1");
            Assert.False(result.IsValid);
            Assert.Equal("Password must include at least one special character (e.g. !@#$).", result.ErrorMessage);
        }

        [Theory]
        [InlineData("Abcdef1!")]        // exactly 8 chars — lower boundary
        [InlineData("MyP@ssw0rd!")]     // common strong form
        [InlineData("X9#aaaaaaaaaaaa")] // multiple filler chars
        public void ValidatePassword_ValidInputs_ReturnsOk(string password)
        {
            var result = ValidationHelper.ValidatePassword(password);
            Assert.True(result.IsValid);
        }

        // ── ValidatePasswordsMatch ───────────────────────────────────────────────

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void ValidatePasswordsMatch_EmptyConfirm_ReturnsFail(string? confirm)
        {
            var result = ValidationHelper.ValidatePasswordsMatch("SomeP@ss1", confirm!);
            Assert.False(result.IsValid);
            Assert.Equal("Please confirm your password.", result.ErrorMessage);
        }

        [Fact]
        public void ValidatePasswordsMatch_Mismatch_ReturnsFail()
        {
            var result = ValidationHelper.ValidatePasswordsMatch("SomeP@ss1", "DifferentP@ss1");
            Assert.False(result.IsValid);
            Assert.Equal("Passwords do not match.", result.ErrorMessage);
        }

        [Fact]
        public void ValidatePasswordsMatch_Match_ReturnsOk()
        {
            var result = ValidationHelper.ValidatePasswordsMatch("SomeP@ss1", "SomeP@ss1");
            Assert.True(result.IsValid);
        }

        [Fact]
        public void ValidatePasswordsMatch_CaseDifference_ReturnsFail()
        {
            // Password matching is case-sensitive.
            var result = ValidationHelper.ValidatePasswordsMatch("someP@ss1", "SomeP@ss1");
            Assert.False(result.IsValid);
        }

        // ── GetPasswordScore ─────────────────────────────────────────────────────

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void GetPasswordScore_NullOrEmpty_ReturnsZero(string? password)
        {
            Assert.Equal(0, ValidationHelper.GetPasswordScore(password!));
        }

        [Fact]
        public void GetPasswordScore_LowercaseOnly_ReturnsOne()
        {
            // "ab" — below 8-char minimum and no upper/digit/special, but has lowercase → score 1.
            Assert.Equal(1, ValidationHelper.GetPasswordScore("ab"));
        }

        [Fact]
        public void GetPasswordScore_LengthAndLowercase_ReturnsTwo()
        {
            // "abcdefgh" — meets length criterion ✓ and lowercase criterion ✓ → score 2.
            Assert.Equal(2, ValidationHelper.GetPasswordScore("abcdefgh"));
        }

        [Fact]
        public void GetPasswordScore_LengthLowercaseUppercase_ReturnsThree()
        {
            // "Abcdefgh" → length ✓, lower ✓, upper ✓, no digit, no special → 3
            Assert.Equal(3, ValidationHelper.GetPasswordScore("Abcdefgh"));
        }

        [Fact]
        public void GetPasswordScore_LengthLowercaseUppercaseDigit_ReturnsFour()
        {
            // "Abcdefg1" → length ✓, lower ✓, upper ✓, digit ✓, no special → 4
            Assert.Equal(4, ValidationHelper.GetPasswordScore("Abcdefg1"));
        }

        [Fact]
        public void GetPasswordScore_AllCriteriaMet_ReturnsFive()
        {
            // "Abcdefg1!" → all 5 criteria → score 5
            Assert.Equal(5, ValidationHelper.GetPasswordScore("Abcdefg1!"));
        }

        // ── GetPasswordStrength ──────────────────────────────────────────────────

        [Fact]
        public void GetPasswordStrength_ScoreZeroOrOne_ReturnsWeak()
        {
            Assert.Equal(PasswordStrength.Weak, ValidationHelper.GetPasswordStrength("a"));
        }

        [Fact]
        public void GetPasswordStrength_ScoreTwo_ReturnsPoor()
        {
            Assert.Equal(PasswordStrength.Poor, ValidationHelper.GetPasswordStrength("abcdefgh"));
        }

        [Fact]
        public void GetPasswordStrength_ScoreThree_ReturnsFair()
        {
            Assert.Equal(PasswordStrength.Fair, ValidationHelper.GetPasswordStrength("Abcdefgh"));
        }

        [Fact]
        public void GetPasswordStrength_ScoreFour_ReturnsStrong()
        {
            Assert.Equal(PasswordStrength.Strong, ValidationHelper.GetPasswordStrength("Abcdefg1"));
        }

        [Fact]
        public void GetPasswordStrength_ScoreFive_ReturnsVeryStrong()
        {
            Assert.Equal(PasswordStrength.VeryStrong, ValidationHelper.GetPasswordStrength("Abcdefg1!"));
        }

        // ── GetMissingCriteriaHint ───────────────────────────────────────────────

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void GetMissingCriteriaHint_NullOrEmpty_ReturnsNull(string? password)
        {
            Assert.Null(ValidationHelper.GetMissingCriteriaHint(password!));
        }

        [Fact]
        public void GetMissingCriteriaHint_SingleChar_ListsAllFiveMissingItems()
        {
            // "a" satisfies only the lowercase criterion — the other four (length, uppercase,
            // digit, special char) are missing.
            string hint = ValidationHelper.GetMissingCriteriaHint("a");
            Assert.NotNull(hint);
            Assert.StartsWith("Missing:", hint);
            Assert.Contains("8+ chars",    hint);
            Assert.Contains("uppercase",   hint);
            Assert.Contains("digit",       hint);
            Assert.Contains("special char", hint);
        }

        [Fact]
        public void GetMissingCriteriaHint_SomeMissing_ListsMissingOnly()
        {
            // "Abcdefgh" — missing digit and special char only.
            string hint = ValidationHelper.GetMissingCriteriaHint("Abcdefgh");
            Assert.Contains("digit",       hint);
            Assert.Contains("special char", hint);
            Assert.DoesNotContain("8+ chars",  hint);
            Assert.DoesNotContain("uppercase", hint);
            Assert.DoesNotContain("lowercase", hint);
        }

        [Fact]
        public void GetMissingCriteriaHint_AllCriteriaMet_ReturnsSuccessMessage()
        {
            string hint = ValidationHelper.GetMissingCriteriaHint("Abcdefg1!");
            Assert.Equal("All requirements met ✓", hint);
        }

        // ── GenerateStrongPassword ───────────────────────────────────────────────

        [Fact]
        public void GenerateStrongPassword_DefaultLength_Returns16Chars()
        {
            Assert.Equal(16, ValidationHelper.GenerateStrongPassword().Length);
        }

        [Fact]
        public void GenerateStrongPassword_CustomLength_ReturnsRequestedLength()
        {
            Assert.Equal(20, ValidationHelper.GenerateStrongPassword(20).Length);
        }

        [Fact]
        public void GenerateStrongPassword_LengthBelow12_ClampsTo12()
        {
            Assert.Equal(12, ValidationHelper.GenerateStrongPassword(5).Length);
        }

        [Fact]
        public void GenerateStrongPassword_AlwaysScoresFive()
        {
            for (int i = 0; i < 20; i++)
                Assert.Equal(5, ValidationHelper.GetPasswordScore(ValidationHelper.GenerateStrongPassword()));
        }

        [Fact]
        public void GenerateStrongPassword_AlwaysPassesPasswordValidation()
        {
            for (int i = 0; i < 10; i++)
            {
                string pw = ValidationHelper.GenerateStrongPassword();
                Assert.True(ValidationHelper.ValidatePassword(pw).IsValid,
                    $"Generated password '{pw}' failed ValidatePassword.");
            }
        }

        [Fact]
        public void GenerateStrongPassword_TwoCallsProduceDifferentResults()
        {
            // The generator must be random; two calls should virtually never match.
            string p1 = ValidationHelper.GenerateStrongPassword();
            string p2 = ValidationHelper.GenerateStrongPassword();
            Assert.NotEqual(p1, p2);
        }

        [Theory]
        [InlineData(12)]
        [InlineData(16)]
        [InlineData(24)]
        [InlineData(32)]
        public void GenerateStrongPassword_VariousLengths_AllPassValidation(int length)
        {
            string pw = ValidationHelper.GenerateStrongPassword(length);
            Assert.Equal(length, pw.Length);
            Assert.True(ValidationHelper.ValidatePassword(pw).IsValid);
        }
    }
}
