using Android.App;
using Android.Content;
using Android.Util;

namespace SecurioClient
{
    // Listens for BOOT_COMPLETED and MY_PACKAGE_REPLACED so that
    // PasswordMonitorService is restarted automatically after a device reboot
    // or an app update — without requiring the user to manually open the app again.
    [BroadcastReceiver(Exported = true)]
    [IntentFilter(new[]
    {
        Intent.ActionBootCompleted,
        Intent.ActionMyPackageReplaced
    })]
    public class BootReceiver : BroadcastReceiver
    {
        private const string Tag = "BootReceiver";

        public override void OnReceive(Context context, Intent intent)
        {
            Log.Info(Tag, $"Received broadcast: {intent?.Action}");
            var serviceIntent = new Intent(context, typeof(PasswordMonitorService));
            context.StartForegroundService(serviceIntent);
        }
    }
}
