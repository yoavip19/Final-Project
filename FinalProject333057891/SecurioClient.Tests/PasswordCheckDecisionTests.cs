using SecurioClient.Helpers;
using SecurioModels.DataTransferObjects;
using Xunit;

namespace SecurioClient.Tests
{
    // Unit tests for PasswordCheckDecision — the pure, platform-independent helper
    // that evaluates a PasswordCheckResult and builds the notification message.
    // No Android SDK is required because PasswordCheckDecision contains no Android references.
    //
    // To run: dotnet test SecurioClient.Tests/SecurioClient.Tests.csproj
    public class PasswordCheckDecisionTests
    {
        // ── ShouldNotify ──────────────────────────────────────────────────────────

        [Fact]
        public void ShouldNotify_NullResult_ReturnsFalse()
        {
            Assert.False(PasswordCheckDecision.ShouldNotify(null));
        }

        [Fact]
        public void ShouldNotify_AllZeroAndFalse_ReturnsFalse()
        {
            var result = new PasswordCheckResult
            {
                BreachedCount    = 0,
                OldCount         = 0,
                MasterPasswordOld = false
            };
            Assert.False(PasswordCheckDecision.ShouldNotify(result));
        }

        [Fact]
        public void ShouldNotify_OnlyBreachedCount_ReturnsTrue()
        {
            var result = new PasswordCheckResult { BreachedCount = 1, OldCount = 0, MasterPasswordOld = false };
            Assert.True(PasswordCheckDecision.ShouldNotify(result));
        }

        [Fact]
        public void ShouldNotify_OnlyOldCount_ReturnsTrue()
        {
            var result = new PasswordCheckResult { BreachedCount = 0, OldCount = 3, MasterPasswordOld = false };
            Assert.True(PasswordCheckDecision.ShouldNotify(result));
        }

        [Fact]
        public void ShouldNotify_OnlyMasterPasswordOld_ReturnsTrue()
        {
            var result = new PasswordCheckResult { BreachedCount = 0, OldCount = 0, MasterPasswordOld = true };
            Assert.True(PasswordCheckDecision.ShouldNotify(result));
        }

        [Fact]
        public void ShouldNotify_AllIssues_ReturnsTrue()
        {
            var result = new PasswordCheckResult { BreachedCount = 2, OldCount = 1, MasterPasswordOld = true };
            Assert.True(PasswordCheckDecision.ShouldNotify(result));
        }

        [Fact]
        public void ShouldNotify_BreachedAndOld_ReturnsTrue()
        {
            var result = new PasswordCheckResult { BreachedCount = 5, OldCount = 3, MasterPasswordOld = false };
            Assert.True(PasswordCheckDecision.ShouldNotify(result));
        }

        // ── BuildMessage ──────────────────────────────────────────────────────────

        [Fact]
        public void BuildMessage_NullResult_ReturnsEmpty()
        {
            Assert.Equal(string.Empty, PasswordCheckDecision.BuildMessage(null));
        }

        [Fact]
        public void BuildMessage_NoIssues_ReturnsEmpty()
        {
            var result = new PasswordCheckResult { BreachedCount = 0, OldCount = 0, MasterPasswordOld = false };
            Assert.Equal(string.Empty, PasswordCheckDecision.BuildMessage(result));
        }

        [Fact]
        public void BuildMessage_OneLeaked_ContainsSingularForm()
        {
            var result = new PasswordCheckResult { BreachedCount = 1, OldCount = 0, MasterPasswordOld = false };
            var msg = PasswordCheckDecision.BuildMessage(result);
            Assert.Contains("1 leaked password", msg);
            // Should NOT say "passwords" (plural)
            Assert.DoesNotContain("1 leaked passwords", msg);
        }

        [Fact]
        public void BuildMessage_TwoLeaked_ContainsPluralForm()
        {
            var result = new PasswordCheckResult { BreachedCount = 2, OldCount = 0, MasterPasswordOld = false };
            var msg = PasswordCheckDecision.BuildMessage(result);
            Assert.Contains("2 leaked passwords", msg);
        }

        [Fact]
        public void BuildMessage_OneOld_ContainsSingularForm()
        {
            var result = new PasswordCheckResult { BreachedCount = 0, OldCount = 1, MasterPasswordOld = false };
            var msg = PasswordCheckDecision.BuildMessage(result);
            Assert.Contains("1 old password", msg);
            Assert.DoesNotContain("1 old passwords", msg);
        }

        [Fact]
        public void BuildMessage_ThreeOld_ContainsPluralForm()
        {
            var result = new PasswordCheckResult { BreachedCount = 0, OldCount = 3, MasterPasswordOld = false };
            var msg = PasswordCheckDecision.BuildMessage(result);
            Assert.Contains("3 old passwords", msg);
        }

        [Fact]
        public void BuildMessage_MasterPasswordOld_ContainsMasterPasswordText()
        {
            var result = new PasswordCheckResult { BreachedCount = 0, OldCount = 0, MasterPasswordOld = true };
            var msg = PasswordCheckDecision.BuildMessage(result);
            Assert.Contains("master password", msg);
            Assert.Contains("90+", msg);
        }

        [Fact]
        public void BuildMessage_AllIssues_ContainsAllParts()
        {
            var result = new PasswordCheckResult { BreachedCount = 2, OldCount = 1, MasterPasswordOld = true };
            var msg = PasswordCheckDecision.BuildMessage(result);
            Assert.Contains("2 leaked passwords", msg);
            Assert.Contains("1 old password",    msg);
            Assert.Contains("master password",   msg);
        }

        [Fact]
        public void BuildMessage_StartsWithSecurioAlert()
        {
            var result = new PasswordCheckResult { BreachedCount = 1, OldCount = 0, MasterPasswordOld = false };
            var msg = PasswordCheckDecision.BuildMessage(result);
            Assert.StartsWith("Securio alert:", msg);
        }

        [Fact]
        public void BuildMessage_EndsWithPeriod()
        {
            var result = new PasswordCheckResult { BreachedCount = 1, OldCount = 0, MasterPasswordOld = false };
            var msg = PasswordCheckDecision.BuildMessage(result);
            Assert.EndsWith(".", msg);
        }

        // ── ShouldNotify / BuildMessage consistency ───────────────────────────────

        [Fact]
        public void WhenShouldNotifyFalse_BuildMessageIsEmpty()
        {
            var result = new PasswordCheckResult { BreachedCount = 0, OldCount = 0, MasterPasswordOld = false };
            bool shouldNotify = PasswordCheckDecision.ShouldNotify(result);
            string message    = PasswordCheckDecision.BuildMessage(result);
            Assert.False(shouldNotify);
            Assert.Equal(string.Empty, message);
        }

        [Fact]
        public void WhenShouldNotifyTrue_BuildMessageIsNonEmpty()
        {
            var result = new PasswordCheckResult { BreachedCount = 3, OldCount = 0, MasterPasswordOld = false };
            bool shouldNotify = PasswordCheckDecision.ShouldNotify(result);
            string message    = PasswordCheckDecision.BuildMessage(result);
            Assert.True(shouldNotify);
            Assert.NotEmpty(message);
        }

        // ── Edge values ───────────────────────────────────────────────────────────

        [Fact]
        public void ShouldNotify_VeryLargeBreachCount_ReturnsTrue()
        {
            var result = new PasswordCheckResult { BreachedCount = int.MaxValue, OldCount = 0, MasterPasswordOld = false };
            Assert.True(PasswordCheckDecision.ShouldNotify(result));
        }

        [Fact]
        public void BuildMessage_LargeNumbers_FormattedCorrectly()
        {
            var result = new PasswordCheckResult { BreachedCount = 100, OldCount = 50, MasterPasswordOld = false };
            var msg = PasswordCheckDecision.BuildMessage(result);
            Assert.Contains("100 leaked passwords", msg);
            Assert.Contains("50 old passwords", msg);
        }
    }
}
