using Android.App;
using Android.Content;
using Android.Util;

namespace SecurioClient
{
    // Receives BOOT_COMPLETED and MY_PACKAGE_REPLACED broadcasts so that the
    // PasswordMonitorService is automatically restarted after a device reboot or
    // an app update — without requiring the user to open the app first.
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

        public override void OnReceive(Context context, Intent intent)
        {
            Log.Info(Tag, $"Received broadcast: {intent?.Action}");
            var serviceIntent = new Intent(context, typeof(PasswordMonitorService));
            context.StartForegroundService(serviceIntent);
        }
    }
}
