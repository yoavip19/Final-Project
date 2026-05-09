using Android.App;
using Android.Content;
using Android.OS;
using SecurioClient.Helpers;
using SecurioClient.Helpers.ServerHelpers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SecurioClient
{
    /// <summary>Foreground service that executes password-monitor and clipboard-protection commands.</summary>
    [Service(Name = ServiceName, Exported = false)]
    public class PasswordMonitorService : Service
    {
        private const long IntervalMs = 24 * 60 * 60 * 1000L; // 24 hours
        private const long ClipboardClearDelayMs = 60_000L; // 60 seconds

        public const string ServiceName = "com.companyname.securioclient.PasswordMonitorService";
        internal const string ActionStartMonitoring = ServiceName + ".action.START_MONITORING";
        internal const string ActionRunPasswordCheck = ServiceName + ".action.RUN_PASSWORD_CHECK";
        internal const string ActionScheduleClipboardClear = ServiceName + ".action.SCHEDULE_CLIPBOARD_CLEAR";
        internal const string ActionClearClipboard = ServiceName + ".action.CLEAR_CLIPBOARD";

        private Handler _handler;
        private Java.Lang.Runnable _passwordCheckRunnable;
        private Java.Lang.Runnable _clipboardClearRunnable;
        private ScreenOffReceiver _screenOffReceiver;
        private bool _isScreenOffReceiverRegistered;
        private IDictionary<string, Action> _commands;

        public override void OnCreate()
        {
            base.OnCreate();

            NotificationHelper.CreateChannel(this);
            StartForeground(
                NotificationHelper.ForegroundNotificationId,
                NotificationHelper.BuildForegroundNotification(this));

            _handler = new Handler(Looper.MainLooper);
            _commands = new Dictionary<string, Action>(StringComparer.Ordinal)
            {
                [ActionStartMonitoring] = StartMonitoringLoop,
                [ActionRunPasswordCheck] = () => _ = RunPasswordCheckAsync(),
                [ActionScheduleClipboardClear] = ScheduleClipboardClear,
                [ActionClearClipboard] = ClearClipboardNow
            };
        }

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            RegisterScreenOffReceiver();

            var action = intent?.Action ?? ActionStartMonitoring;
            if (!_commands.TryGetValue(action, out var command))
                command = _commands[ActionStartMonitoring];

            command();
            return StartCommandResult.Sticky;
        }

        public override IBinder OnBind(Intent intent) => null;

        public override void OnDestroy()
        {
            CancelPasswordCheck();
            CancelClipboardClear();
            UnregisterScreenOffReceiver();
            base.OnDestroy();
        }

        public static void StartMonitoring(Context context)
            => StartCommand(context, ActionStartMonitoring);

        internal static void ScheduleClipboardClear(Context context)
            => StartCommand(context, ActionScheduleClipboardClear);

        internal static void ClearClipboard(Context context)
            => StartCommand(context, ActionClearClipboard);

        private void RegisterScreenOffReceiver()
        {
            if (_isScreenOffReceiverRegistered) return;
            _screenOffReceiver ??= new ScreenOffReceiver();
            RegisterReceiver(_screenOffReceiver, new IntentFilter(Intent.ActionScreenOff));
            _isScreenOffReceiverRegistered = true;
        }

        private void UnregisterScreenOffReceiver()
        {
            if (!_isScreenOffReceiverRegistered) return;
            if (_screenOffReceiver == null) return;
            try { UnregisterReceiver(_screenOffReceiver); }
            catch (Java.Lang.IllegalArgumentException) { /* already unregistered */ }
            finally
            {
                _isScreenOffReceiverRegistered = false;
                _screenOffReceiver = null;
            }
        }

        private static void StartCommand(Context context, string action)
        {
            var appContext = context.ApplicationContext;
            var intent = new Intent(appContext, typeof(PasswordMonitorService));
            intent.SetAction(action);
            appContext.StartForegroundService(intent);
        }

        private void StartMonitoringLoop()
        {
            ScheduleNextPasswordCheck(immediate: true);
        }

        private void ScheduleNextPasswordCheck(bool immediate)
        {
            CancelPasswordCheck();
            _passwordCheckRunnable = new Java.Lang.Runnable(() => StartCommand(this, ActionRunPasswordCheck));
            if (immediate)
                _handler.Post(_passwordCheckRunnable);
            else
                _handler.PostDelayed(_passwordCheckRunnable, IntervalMs);
        }

        private void CancelPasswordCheck()
        {
            if (_passwordCheckRunnable != null)
                _handler?.RemoveCallbacks(_passwordCheckRunnable);
            _passwordCheckRunnable = null;
        }

        private async Task RunPasswordCheckAsync()
        {
            if (NotificationHelper.AreNotificationsEnabled(this))
                await PerformCheckAsync(this);

            ScheduleNextPasswordCheck(immediate: false);
        }

        private void ScheduleClipboardClear()
        {
            CancelClipboardClear();
            _clipboardClearRunnable = new Java.Lang.Runnable(() => StartCommand(this, ActionClearClipboard));
            _handler.PostDelayed(_clipboardClearRunnable, ClipboardClearDelayMs);
        }

        private void CancelClipboardClear()
        {
            if (_clipboardClearRunnable != null)
                _handler?.RemoveCallbacks(_clipboardClearRunnable);
            _clipboardClearRunnable = null;
        }

        private void ClearClipboardNow()
        {
            CancelClipboardClear();
            ClipboardGuard.ClearNow(this);
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
                    ClearClipboard(context);
            }
        }
    }
}
