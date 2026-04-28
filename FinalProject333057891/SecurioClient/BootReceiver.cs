using Android.App;
using Android.Content;
using Android.Util;

namespace SecurioClient
{
    /// <summary>Receives BOOT_COMPLETED and MY_PACKAGE_REPLACED broadcasts to restart the PasswordMonitorService after reboot or update.</summary>
    [BroadcastReceiver(
        Exported = true,
        DirectBootAware = false)]
    [IntentFilter(new[]
    {
        Intent.ActionBootCompleted,
        Intent.ActionMyPackageReplaced
    })]
    public class BootReceiver : BroadcastReceiver
    {
        private const string Tag = "BootReceiver";

        /// <summary>Handles the broadcast and starts the PasswordMonitorService as a foreground service.</summary>
        public override void OnReceive(Context context, Intent intent)
        {
            Log.Info(Tag, $"Received broadcast: {intent?.Action}");
            var serviceIntent = new Intent(context, typeof(PasswordMonitorService));
            context.StartForegroundService(serviceIntent);
        }
    }
}
