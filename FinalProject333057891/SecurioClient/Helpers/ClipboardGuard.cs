using Android.Content;
using Android.OS;

namespace SecurioClient.Helpers
{
    /// <summary>Manages sensitive-clipboard hygiene by marking password clips as sensitive, scheduling a 60-second auto-purge, and exposing ClearNow for immediate clearing.</summary>
    internal static class ClipboardGuard
    {
        private const string ExtraIsSensitive = "android.content.extra.IS_SENSITIVE";
        private const string ExtraGboardCanSkipHistory = "com.google.android.inputmethod.latin.CAN_SKIP_HISTORY";
        /// <summary>Copies the password to the clipboard with the sensitive flag set, then asks the foreground monitor service to restart the 60-second auto-purge timer.</summary>
        internal static void CopySensitive(Context context, string password)
        {
            var clipboard = (ClipboardManager)context.GetSystemService(Context.ClipboardService);
            if (clipboard == null) return;
            var clip = ClipData.NewPlainText("password", password);

            var extras = new PersistableBundle();
            extras.PutBoolean(ExtraIsSensitive, true);
            extras.PutBoolean(ExtraGboardCanSkipHistory, true);
            clip.Description.Extras = extras;

            clipboard.PrimaryClip = clip;
            PasswordMonitorService.ScheduleClipboardClear(context);
        }

        /// <summary>Immediately clears the system clipboard using ClearPrimaryClip to avoid triggering the system overlay notification.</summary>
        internal static void ClearNow(Context context)
        {
            var clipboard = (ClipboardManager)context.GetSystemService(Context.ClipboardService);
            clipboard?.ClearPrimaryClip();
        }
    }
}
