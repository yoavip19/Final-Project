using Android.Content;

namespace SecurioClient
{
    /// <summary>Dynamic receiver that clears clipboard content when the screen turns off.</summary>
    internal sealed class ScreenOffClipboardReceiver : BroadcastReceiver
    {
        /// <summary>Handles broadcasts and clears clipboard content on ACTION_SCREEN_OFF.</summary>
        /// <param name="context">Broadcast context.</param>
        /// <param name="intent">Broadcast intent.</param>
        public override void OnReceive(Context context, Intent intent)
        {
            if (intent?.Action == Intent.ActionScreenOff)
                PasswordMonitorService.ClearClipboard(context);
        }
    }
}
