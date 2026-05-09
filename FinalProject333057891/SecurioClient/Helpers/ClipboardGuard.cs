using Android.Content;

namespace SecurioClient.Helpers
{
    /// <summary>
    /// Manages sensitive-clipboard hygiene for the password manager:
    /// <list type="bullet">
    ///   <item>Marks password clips with <c>EXTRA_IS_SENSITIVE</c> so modern IMEs skip their
    ///         internal history buffers (Gboard, Samsung Keyboard, etc.).</item>
    ///   <item>Requests a service-owned 60-second auto-purge after each copy, with debounce
    ///         so rapid successive copies reset the timer.</item>
    ///   <item>Exposes <see cref="ClearNow"/> for the service's clear commands.</item>
    /// </list>
    /// </summary>
    internal static class ClipboardGuard
    {
        // Value of ClipDescription.EXTRA_IS_SENSITIVE (added in API 33).
        private const string ExtraIsSensitive = "android.content.extra.IS_SENSITIVE";
        // Undocumented Gboard-specific hint to skip saving this clip in keyboard history.
        // This is not part of a stable public API and may be ignored by other keyboards/future versions.
        // We still set it as a best-effort complement to EXTRA_IS_SENSITIVE because no public Android API
        // exists for purging third-party keyboard history after the copy already happened.
        private const string ExtraGboardCanSkipHistory = "com.google.android.inputmethod.latin.CAN_SKIP_HISTORY";
        /// <summary>
        /// Copies <paramref name="password"/> to the clipboard with the sensitive flag set,
        /// then asks the foreground monitor service to (re-)start the 60-second auto-purge timer.
        /// </summary>
        internal static void CopySensitive(Context context, string password)
        {
            var clipboard = (ClipboardManager)context.GetSystemService(Context.ClipboardService);
            if (clipboard == null) return;
            var clip = ClipData.NewPlainText("password", password);

            // Instruct modern IMEs to bypass their history / suggestion buffers.
            var extras = new PersistableBundle();
            extras.PutBoolean(ExtraIsSensitive, true);
            extras.PutBoolean(ExtraGboardCanSkipHistory, true);
            clip.Description.Extras = extras;

            clipboard.PrimaryClip = clip;
            PasswordMonitorService.ScheduleClipboardClear(context);
        }

        /// <summary>
        /// Immediately clears the system clipboard.
        /// Called by the monitor service when a scheduled or screen-off clear command fires.
        /// <para>
        /// We call <c>ClearPrimaryClip()</c> directly rather than first writing an empty clip.
        /// Writing any clip via <c>PrimaryClip = …</c> from a background context on API 33+
        /// triggers the system "Copied to clipboard" overlay; <c>ClearPrimaryClip()</c> does not.
        /// Because <c>minSdkVersion = 33</c>, <c>ClearPrimaryClip()</c> is always available.
        /// </para>
        /// </summary>
        internal static void ClearNow(Context context)
        {
            var clipboard = (ClipboardManager)context.GetSystemService(Context.ClipboardService);
            clipboard?.ClearPrimaryClip();
        }
    }
}
