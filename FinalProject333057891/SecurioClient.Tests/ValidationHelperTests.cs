using SecurioClient.Helpers;
using Xunit;

namespace SecurioClient.Tests
{
    // Comprehensive unit tests for ValidationHelper — the client-side field-validation
    // and password-strength utility.  Pure .NET; no Android dependencies.
    // Run: dotnet test SecurioClient.Tests/SecurioClient.Tests.csproj
    public class ValidationHelperTests
    {
        // ── ValidateUsername: empty / null ───────────────────────────────────────

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ValidateUsername_NullOrWhitespace_ReturnsFail(string? username)
        {
            var result = ValidationHelper.ValidateUsername(username!);
            Assert.False(result.IsValid);
            Assert.Equal("Username is required.", result.ErrorMessage);
        }

        // ── ValidateUsername: length ─────────────────────────────────────────────

        [Theory]
        [InlineData("a")]
        [InlineData("ab")]
        public void ValidateUsername_TooShort_ReturnsFail(string username)
        {
            var result = ValidationHelper.ValidateUsername(username);
            Assert.False(result.IsValid);
            Assert.Equal("Username must be at least 3 characters.", result.ErrorMessage);
        }

        [Fact]
        public void ValidateUsername_TooLong_ReturnsFail()
        {
            var result = ValidationHelper.ValidateUsername(new string('a', 31));
            Assert.False(result.IsValid);
            Assert.Equal("Username must be 30 characters or fewer.", result.ErrorMessage);
        }

        [Theory]
        [InlineData("abc")]                               // exactly 3
        [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]   // exactly 30
        public void ValidateUsername_BoundaryLengths_ReturnsOk(string username)
        {
            Assert.True(ValidationHelper.ValidateUsername(username).IsValid);
        }

        // ── ValidateUsername: first character ────────────────────────────────────

        [Theory]
        [InlineData("1abc")]
        [InlineData("_abc")]
        [InlineData("-abc")]
        [InlineData("0abc")]
        public void ValidateUsername_StartsWithNonLetter_ReturnsFail(string username)
        {
            var result = ValidationHelper.ValidateUsername(username);
            Assert.False(result.IsValid);
            Assert.Equal("Username must start with a letter.", result.ErrorMessage);
        }

        // ── ValidateUsername: invalid characters ─────────────────────────────────

        [Theory]
        [InlineData("abc!")]
        [InlineData("abc def")]
        [InlineData("abc@123")]
        [InlineData("abc.def")]
        [InlineData("abc#def")]
        public void ValidateUsername_InvalidChars_ReturnsFail(string username)
        {
            var result = ValidationHelper.ValidateUsername(username);
            Assert.False(result.IsValid);
            Assert.Equal("Username may only contain letters, digits, underscores, or hyphens.",
                result.ErrorMessage);
        }

        // ── ValidateUsername: valid inputs ───────────────────────────────────────

        [Theory]
        [InlineData("Alice")]
        [InlineData("user_123")]
        [InlineData("my-name")]
        [InlineData("A1_B2-C3")]
        [InlineData("abc")]
        public void ValidateUsername_ValidInputs_ReturnsOkWithNullMessage(string username)
        {
            var result = ValidationHelper.ValidateUsername(username);
            Assert.True(result.IsValid);
            Assert.Null(result.ErrorMessage);
        }

        // ── ValidateEmail: empty / null ──────────────────────────────────────────

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ValidateEmail_NullOrWhitespace_ReturnsFail(string? email)
        {
            var result = ValidationHelper.ValidateEmail(email!);
            Assert.False(result.IsValid);
            Assert.Equal("Email address is required.", result.ErrorMessage);
        }

        // ── ValidateEmail: invalid format ────────────────────────────────────────

        [Theory]
        [InlineData("notanemail")]
        [InlineData("missing@domain")]
        [InlineData("@nodomain.com")]
        [InlineData("two@@signs.com")]
        [InlineData("spaces in@email.com")]
        [InlineData("noatsign.com")]
        public void ValidateEmail_InvalidFormat_ReturnsFail(string email)
        {
            var result = ValidationHelper.ValidateEmail(email);
            Assert.False(result.IsValid);
            Assert.Equal("Please enter a valid email address.", result.ErrorMessage);
        }

        // ── ValidateEmail: valid inputs ──────────────────────────────────────────

        [Theory]
        [InlineData("user@example.com")]
        [InlineData("user.name+tag@sub.domain.org")]
        [InlineData("USER@EXAMPLE.COM")]
        [InlineData("user123@test.co.uk")]
        [InlineData("a@b.io")]
        public void ValidateEmail_ValidInputs_ReturnsOk(string email)
        {
            Assert.True(ValidationHelper.ValidateEmail(email).IsValid);
        }

        // ── ValidatePassword: empty / null ───────────────────────────────────────

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void ValidatePassword_NullOrEmpty_ReturnsFail(string? password)
        {
            var result = ValidationHelper.ValidatePassword(password!);
            Assert.False(result.IsValid);
            Assert.Equal("Password is required.", result.ErrorMessage);
        }

        // ── ValidatePassword: rule failures ──────────────────────────────────────

        [Fact]
        public void ValidatePassword_TooShort_ReturnsFail()
        {
            var r = ValidationHelper.ValidatePassword("Ab1!567"); // 7 chars
            Assert.False(r.IsValid);
            Assert.Equal("Password must be at least 8 characters.", r.ErrorMessage);
        }

        [Fact]
        public void ValidatePassword_NoUppercase_ReturnsFail()
        {
            Assert.False(ValidationHelper.ValidatePassword("abc12345!").IsValid);
        }

        [Fact]
        public void ValidatePassword_NoLowercase_ReturnsFail()
        {
            Assert.False(ValidationHelper.ValidatePassword("ABC12345!").IsValid);
        }

        [Fact]
        public void ValidatePassword_NoDigit_ReturnsFail()
        {
            Assert.False(ValidationHelper.ValidatePassword("Abcdefgh!").IsValid);
        }

        [Fact]
        public void ValidatePassword_NoSpecialChar_ReturnsFail()
        {
            Assert.False(ValidationHelper.ValidatePassword("Abcdefg1").IsValid);
        }

        // ── ValidatePassword: valid inputs ───────────────────────────────────────

        [Theory]
        [InlineData("Abcdef1!")]        // minimum 8 chars
        [InlineData("MyP@ssw0rd!")]
        [InlineData("X9#aaaaaaaaaaaaa")]
        [InlineData("Str0ng!Pass#word")]
        public void ValidatePassword_ValidInputs_ReturnsOk(string password)
        {
            Assert.True(ValidationHelper.ValidatePassword(password).IsValid);
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
        public void ValidatePasswordsMatch_CaseDifference_ReturnsFail()
        {
            var result = ValidationHelper.ValidatePasswordsMatch("someP@ss1", "SomeP@ss1");
            Assert.False(result.IsValid);
        }

        [Fact]
        public void ValidatePasswordsMatch_Match_ReturnsOk()
        {
            Assert.True(ValidationHelper.ValidatePasswordsMatch("SomeP@ss1", "SomeP@ss1").IsValid);
        }

        [Fact]
        public void ValidatePasswordsMatch_BothSameComplexPassword_ReturnsOk()
        {
            const string complex = "C0mpl3x!P@ssw0rd#2024";
            Assert.True(ValidationHelper.ValidatePasswordsMatch(complex, complex).IsValid);
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
            // Only lowercase criterion met (length < 8).
            Assert.Equal(1, ValidationHelper.GetPasswordScore("ab"));
        }

        [Fact]
        public void GetPasswordScore_LengthAndLowercase_ReturnsTwo()
        {
            Assert.Equal(2, ValidationHelper.GetPasswordScore("abcdefgh"));
        }

        [Fact]
        public void GetPasswordScore_LengthLowercaseUpper_ReturnsThree()
        {
            Assert.Equal(3, ValidationHelper.GetPasswordScore("Abcdefgh"));
        }

        [Fact]
        public void GetPasswordScore_LengthLowercaseUpperDigit_ReturnsFour()
        {
            Assert.Equal(4, ValidationHelper.GetPasswordScore("Abcdefg1"));
        }

        [Fact]
        public void GetPasswordScore_AllCriteria_ReturnsFive()
        {
            Assert.Equal(5, ValidationHelper.GetPasswordScore("Abcdefg1!"));
        }

        [Fact]
        public void GetPasswordScore_UpperOnly_ReturnsOne()
        {
            Assert.Equal(1, ValidationHelper.GetPasswordScore("AB"));
        }

        // ── GetPasswordStrength ──────────────────────────────────────────────────

        [Fact]
        public void GetPasswordStrength_ScoreZero_ReturnsWeak()
        {
            Assert.Equal(PasswordStrength.Weak, ValidationHelper.GetPasswordStrength(null!));
        }

        [Fact]
        public void GetPasswordStrength_ScoreOne_ReturnsWeak()
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
        public void GetMissingCriteriaHint_AllMissing_ListsAllItems()
        {
            // Single lowercase char satisfies no criterion other than lowercase.
            string hint = ValidationHelper.GetMissingCriteriaHint("a");
            Assert.NotNull(hint);
            Assert.StartsWith("Missing:", hint);
            Assert.Contains("8+ chars", hint);
            Assert.Contains("uppercase", hint);
            Assert.Contains("digit", hint);
            Assert.Contains("special char", hint);
        }

        [Fact]
        public void GetMissingCriteriaHint_OnlyMissingDigitAndSpecial_DoesNotListOthers()
        {
            string hint = ValidationHelper.GetMissingCriteriaHint("Abcdefgh");
            Assert.Contains("digit", hint);
            Assert.Contains("special char", hint);
            Assert.DoesNotContain("8+ chars", hint);
            Assert.DoesNotContain("uppercase", hint);
            Assert.DoesNotContain("lowercase", hint);
        }

        [Fact]
        public void GetMissingCriteriaHint_AllCriteriaMet_ReturnsSuccessMessage()
        {
            Assert.Equal("All requirements met ✓", ValidationHelper.GetMissingCriteriaHint("Abcdefg1!"));
        }

        [Fact]
        public void GetMissingCriteriaHint_MissingUppercase_ListsUppercaseOnly()
        {
            // "abcdefg1!" — has length, lowercase, digit, special; missing uppercase.
            string hint = ValidationHelper.GetMissingCriteriaHint("abcdefg1!");
            Assert.Contains("uppercase", hint);
            Assert.DoesNotContain("8+ chars", hint);
            Assert.DoesNotContain("digit", hint);
            Assert.DoesNotContain("special char", hint);
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
            for (int i = 0; i < 25; i++)
                Assert.Equal(5, ValidationHelper.GetPasswordScore(ValidationHelper.GenerateStrongPassword()));
        }

        [Fact]
        public void GenerateStrongPassword_AlwaysPassesValidation()
        {
            for (int i = 0; i < 10; i++)
                Assert.True(ValidationHelper.ValidatePassword(ValidationHelper.GenerateStrongPassword()).IsValid);
        }

        [Fact]
        public void GenerateStrongPassword_ConsecutiveCallsProduceDifferentValues()
        {
            // Collision probability on a 16-char space is negligible.
            string p1 = ValidationHelper.GenerateStrongPassword();
            string p2 = ValidationHelper.GenerateStrongPassword();
            Assert.NotEqual(p1, p2);
        }

        [Fact]
        public void GenerateStrongPassword_LengthExactly12_ReturnsExactly12Chars()
        {
            Assert.Equal(12, ValidationHelper.GenerateStrongPassword(12).Length);
        }
    }
}
