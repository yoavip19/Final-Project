using Android.Content;
using Android.OS;

namespace SecurioClient.Helpers
{
    /// <summary>
    /// Manages sensitive-clipboard hygiene for the password manager:
    /// <list type="bullet">
    ///   <item>Marks password clips with <c>EXTRA_IS_SENSITIVE</c> so modern IMEs skip their
    ///         internal history buffers (Gboard, Samsung Keyboard, etc.).</item>
    ///   <item>Schedules an automatic clipboard purge 60 seconds after each copy, with debounce
    ///         so rapid successive copies reset the timer.</item>
    ///   <item>Exposes <see cref="ClearNow"/> for the screen-off emergency purge.</item>
    /// </list>
    /// </summary>
    internal static class ClipboardGuard
    {
        // Value of ClipDescription.EXTRA_IS_SENSITIVE (added in API 33).
        private const string ExtraIsSensitive = "android.content.extra.IS_SENSITIVE";
        // Undocumented Gboard-specific hint to skip saving this clip in keyboard history.
        // This is not part of a stable public API and may be ignored by other keyboards/future versions.
        private const string ExtraGboardCanSkipHistory = "com.google.android.inputmethod.latin.CAN_SKIP_HISTORY";
        private const long ClearDelayMs = 60_000L; // 60 seconds

        private static readonly object _sync = new object();
        private static readonly Handler _handler = new Handler(Looper.MainLooper);
        private static Java.Lang.Runnable _clearRunnable;

        /// <summary>
        /// Copies <paramref name="password"/> to the clipboard with the sensitive flag set,
        /// then (re-)starts the 60-second auto-purge timer.
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
            ScheduleClear(context);
        }

        /// <summary>
        /// (Re-)schedules a clipboard purge 60 seconds from now.
        /// Calling this again before the timer fires resets the countdown (debounce).
        /// </summary>
        internal static void ScheduleClear(Context context)
        {
            // Use application context so the lambda does not retain an Activity reference.
            var appContext = context.ApplicationContext;
            lock (_sync)
            {
                if (_clearRunnable != null)
                    _handler.RemoveCallbacks(_clearRunnable);
                _clearRunnable = new Java.Lang.Runnable(() => ClearNow(appContext));
                _handler.PostDelayed(_clearRunnable, ClearDelayMs);
            }
        }

        /// <summary>
        /// Immediately clears the system clipboard and cancels any pending auto-purge timer.
        /// Called both by the scheduled runnable and by the screen-off receiver.
        /// </summary>
        internal static void ClearNow(Context context)
        {
            lock (_sync)
            {
                if (_clearRunnable != null)
                    _handler.RemoveCallbacks(_clearRunnable);
                _clearRunnable = null;
            }
            var clipboard = (ClipboardManager)context.GetSystemService(Context.ClipboardService);
            if (clipboard == null) return;

            // Overwrite first so old IME/history implementations are more likely to evict the previous value.
            clipboard.PrimaryClip = ClipData.NewPlainText(string.Empty, string.Empty);
            clipboard.ClearPrimaryClip();
        }
    }
}
