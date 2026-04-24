namespace SecurioClient
{
    /// <summary>
    /// Compile-time configuration flags for the testing branch.
    /// Set <see cref="IsTestMode"/> to <c>false</c> before merging to production.
    /// </summary>
    internal static class TestConfig
    {
        /// <summary>
        /// When <c>true</c>, test overrides are active:
        /// short check interval, verbose ADB logging, and the TestDashboard activity
        /// becomes accessible via ADB or a long-press on the profile title.
        /// Automatically disabled in Release builds via the DEBUG compile symbol.
        /// </summary>
#if DEBUG
        public const bool IsTestMode = true;
#else
        public const bool IsTestMode = false;
#endif

        /// <summary>
        /// Password-monitor check interval in milliseconds.
        /// 30 seconds in test mode so the 24-hour cycle can be verified quickly;
        /// 24 hours in production.
        /// </summary>
        public const long CheckIntervalMs = IsTestMode ? 30_000L : 24L * 60 * 60 * 1000;

        /// <summary>ADB log tag used by all test-mode log entries for easy filtering.</summary>
        public const string LogTag = "SecurioTest";
    }
}
