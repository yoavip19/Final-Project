using Android.App;
using Android.Content;
using Android.OS;
using SecurioClient.Helpers;
using SecurioClient.Helpers.ServerHelpers;
using System.Threading.Tasks;

namespace SecurioClient
{
    /// <summary>Foreground service that performs a password-health check every 24 hours and clears the clipboard on screen-off.</summary>
    [Service(Name = "com.companyname.securioclient.MonitorService", Exported = false)]
    public class MonitorService : Service
    {
        private const long IntervalMs = 24 * 60 * 60 * 1000L; // 24 hours

        private Handler _handler;
        private Java.Lang.Runnable _runnable;
        private ScreenOffReceiver _screenOffReceiver;
        private bool _isScreenOffReceiverRegistered;

        public override void OnCreate()
        {
            base.OnCreate();

            NotificationHelper.CreateChannel(this);
            StartForeground(
                NotificationHelper.ForegroundNotificationId,
                NotificationHelper.BuildForegroundNotification(this));

            _handler = new Handler(Looper.MainLooper);
            _runnable = new Java.Lang.Runnable(async () => await RunCycleAsync());
            _handler.Post(_runnable);

            RegisterScreenOffReceiver();
        }

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
            => StartCommandResult.Sticky;

        public override IBinder OnBind(Intent intent) => null;

        public override void OnDestroy()
        {
            _handler?.RemoveCallbacks(_runnable);
            UnregisterScreenOffReceiver();
            base.OnDestroy();
        }

        private void RegisterScreenOffReceiver()
        {
            if (_isScreenOffReceiverRegistered) return;
            _screenOffReceiver ??= new ScreenOffReceiver();
            RegisterReceiver(_screenOffReceiver, new IntentFilter(Intent.ActionScreenOff));
            _isScreenOffReceiverRegistered = true;
        }

        private void UnregisterScreenOffReceiver()
        {
            if (!_isScreenOffReceiverRegistered || _screenOffReceiver == null) return;
            try { UnregisterReceiver(_screenOffReceiver); }
            catch (Java.Lang.IllegalArgumentException) { /* already unregistered */ }
            _isScreenOffReceiverRegistered = false;
            _screenOffReceiver = null;
        }

        private async Task RunCycleAsync()
        {
            if (NotificationHelper.AreNotificationsEnabled(this))
                await PerformCheckAsync(this);

            _handler?.PostDelayed(_runnable, IntervalMs);
        }

        internal static async Task PerformCheckAsync(Context context)
        {
            int userId = await StorageHelper.GetUserId();
            if (userId <= 0) return;

            var result = await PasswordCheckService.FetchAsync(userId);
            if (result == null) return;

            NotificationHelper.PostCheckResult(context, result);
        }

        private sealed class ScreenOffReceiver : BroadcastReceiver
        {
            public override void OnReceive(Context context, Intent intent)
            {
                if (intent?.Action == Intent.ActionScreenOff)
                    ClipboardGuard.ClearNow(context);
            }
        }
    }
}
