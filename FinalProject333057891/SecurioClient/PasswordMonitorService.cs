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
        private const long IntervalMs = 24 * 60 * 60 * 1000L;
        private const long ClipboardClearDelayMs = 60_000L;

        public const string ServiceName = "com.companyname.securioclient.PasswordMonitorService";
        internal const string ActionStartMonitoring = ServiceName + ".action.START_MONITORING";
        internal const string ActionRunPasswordCheck = ServiceName + ".action.RUN_PASSWORD_CHECK";
        internal const string ActionScheduleClipboardClear = ServiceName + ".action.SCHEDULE_CLIPBOARD_CLEAR";
        internal const string ActionClearClipboard = ServiceName + ".action.CLEAR_CLIPBOARD";

        private Handler _handler;
        private Java.Lang.Runnable _passwordCheckRunnable;
        private Java.Lang.Runnable _clipboardClearRunnable;
        private ScreenOffClipboardReceiver _screenOffReceiver;
        private bool _isScreenOffReceiverRegistered;
        private IDictionary<string, Func<Task>> _commands;

        /// <summary>
        /// Initializes the foreground service resources, creates the command map, and prepares reusable
        /// runnables for password-check scheduling and clipboard purge scheduling.
        /// </summary>
        public override void OnCreate()
        {
            base.OnCreate();

            NotificationHelper.CreateChannel(this);
            StartForeground(
                NotificationHelper.ForegroundNotificationId,
                NotificationHelper.BuildForegroundNotification(this));

            _handler = new Handler(Looper.MainLooper);
            _passwordCheckRunnable = new Java.Lang.Runnable(() => StartCommand(this, ActionRunPasswordCheck));
            _clipboardClearRunnable = new Java.Lang.Runnable(() => StartCommand(this, ActionClearClipboard));
            _commands = new Dictionary<string, Func<Task>>(StringComparer.OrdinalIgnoreCase)
            {
                [ActionStartMonitoring] = ExecuteStartMonitoringAsync,
                [ActionRunPasswordCheck] = RunPasswordCheckAsync,
                [ActionScheduleClipboardClear] = ExecuteScheduleClipboardClearAsync,
                [ActionClearClipboard] = ExecuteClearClipboardAsync
            };
        }

        /// <summary>
        /// Handles incoming service actions and routes each action to the appropriate command handler.
        /// Unknown or missing actions fall back to monitor startup handling.
        /// </summary>
        /// <param name="intent">Incoming service intent containing the command action.</param>
        /// <param name="flags">Additional start-request metadata supplied by Android.</param>
        /// <param name="startId">Unique ID for this service start request.</param>
        /// <returns><see cref="StartCommandResult.Sticky"/> so Android recreates the service if needed.</returns>
        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            RegisterScreenOffReceiver();

            _ = ExecuteCommandAsync(intent?.Action ?? ActionStartMonitoring);
            return StartCommandResult.Sticky;
        }

        /// <summary>Binding is not supported because this service is command-driven and runs in the foreground.</summary>
        /// <param name="intent">Binding request intent.</param>
        /// <returns>Always <c>null</c>.</returns>
        public override IBinder OnBind(Intent intent) => null;

        /// <summary>
        /// Cleans up pending callbacks and unregisters dynamic receivers when the service is being destroyed.
        /// </summary>
        public override void OnDestroy()
        {
            CancelPasswordCheck();
            CancelClipboardClear();
            UnregisterScreenOffReceiver();
            base.OnDestroy();
        }

        /// <summary>Starts this service and triggers the monitoring bootstrap command.</summary>
        /// <param name="context">Caller context used to start the foreground service.</param>
        public static void StartMonitoring(Context context)
            => StartCommand(context, ActionStartMonitoring);

        /// <summary>
        /// Requests the clipboard-clear timer command so the service purges the system clipboard in 60 seconds.
        /// </summary>
        /// <param name="context">Caller context used to dispatch the command.</param>
        internal static void ScheduleClipboardClear(Context context)
            => StartCommand(context, ActionScheduleClipboardClear);

        /// <summary>Requests an immediate clipboard purge command.</summary>
        /// <param name="context">Caller context used to dispatch the command.</param>
        internal static void ClearClipboard(Context context)
            => StartCommand(context, ActionClearClipboard);

        /// <summary>Registers the dynamic <c>ACTION_SCREEN_OFF</c> receiver once per service lifetime.</summary>
        private void RegisterScreenOffReceiver()
        {
            if (_isScreenOffReceiverRegistered) return;
            _screenOffReceiver ??= new ScreenOffClipboardReceiver();
            RegisterReceiver(_screenOffReceiver, new IntentFilter(Intent.ActionScreenOff));
            _isScreenOffReceiverRegistered = true;
        }

        /// <summary>Unregisters the dynamic screen-off receiver when the service stops.</summary>
        private void UnregisterScreenOffReceiver()
        {
            if (!_isScreenOffReceiverRegistered) return;
            try { UnregisterReceiver(_screenOffReceiver); }
            catch (Java.Lang.IllegalArgumentException) { }
            finally
            {
                _isScreenOffReceiverRegistered = false;
                _screenOffReceiver = null;
            }
        }

        /// <summary>
        /// Starts this foreground service with the provided action so command handling remains centralized.
        /// </summary>
        /// <param name="context">Caller context.</param>
        /// <param name="action">Action constant representing the command to execute.</param>
        private static void StartCommand(Context context, string action)
        {
            var appContext = context.ApplicationContext;
            var intent = new Intent(appContext, typeof(PasswordMonitorService));
            intent.SetAction(action);
            appContext.StartForegroundService(intent);
        }

        /// <summary>
        /// Executes a command handler for the provided action and applies fallback/error handling behavior.
        /// </summary>
        /// <param name="action">Action key used to resolve the command handler.</param>
        private async Task ExecuteCommandAsync(string action)
        {
            if (!_commands.TryGetValue(action, out var command))
                command = _commands[ActionStartMonitoring];

            try
            {
                await command();
            }
            catch (Exception)
            {
                // Fail-open behavior: only ensure periodic password checks continue scheduling.
                if (action == ActionRunPasswordCheck)
                    ScheduleNextPasswordCheck(immediate: false);
            }
        }

        /// <summary>Command wrapper that starts the recurring password-monitor loop.</summary>
        private Task ExecuteStartMonitoringAsync()
        {
            StartMonitoringLoop();
            return Task.CompletedTask;
        }

        /// <summary>Command wrapper that schedules the delayed clipboard purge callback.</summary>
        private Task ExecuteScheduleClipboardClearAsync()
        {
            ScheduleClipboardClear();
            return Task.CompletedTask;
        }

        /// <summary>Command wrapper that clears the clipboard immediately.</summary>
        private Task ExecuteClearClipboardAsync()
        {
            ClearClipboardNow();
            return Task.CompletedTask;
        }

        /// <summary>Bootstraps the periodic password-check schedule.</summary>
        private void StartMonitoringLoop()
        {
            ScheduleNextPasswordCheck(immediate: true);
        }

        /// <summary>
        /// Schedules the next password-check command either immediately or after the configured interval.
        /// </summary>
        /// <param name="immediate"><c>true</c> to run now; otherwise schedule for the 24-hour interval.</param>
        private void ScheduleNextPasswordCheck(bool immediate)
        {
            CancelPasswordCheck();
            if (immediate)
                _handler.Post(_passwordCheckRunnable);
            else
                _handler.PostDelayed(_passwordCheckRunnable, IntervalMs);
        }

        /// <summary>Cancels any pending password-check callback.</summary>
        private void CancelPasswordCheck()
        {
            CancelCallback(_passwordCheckRunnable);
        }

        /// <summary>
        /// Executes one password-check cycle and schedules the next cycle when complete.
        /// </summary>
        private async Task RunPasswordCheckAsync()
        {
            if (NotificationHelper.AreNotificationsEnabled(this) && NotificationPreferenceHelper.IsEnabled)
                await PerformCheckAsync(this);

            ScheduleNextPasswordCheck(immediate: false);
        }

        /// <summary>Schedules clipboard clearing 60 seconds from now and debounces previous schedules.</summary>
        private void ScheduleClipboardClear()
        {
            CancelClipboardClear();
            _handler.PostDelayed(_clipboardClearRunnable, ClipboardClearDelayMs);
        }

        /// <summary>Cancels any pending clipboard-clear callback.</summary>
        private void CancelClipboardClear()
        {
            CancelCallback(_clipboardClearRunnable);
        }

        /// <summary>
        /// Removes a pending runnable callback from the service handler if both are available.
        /// </summary>
        /// <param name="runnable">Runnable callback to remove.</param>
        private void CancelCallback(Java.Lang.Runnable runnable)
        {
            if (_handler != null && runnable != null)
                _handler.RemoveCallbacks(runnable);
        }

        /// <summary>Clears the system clipboard immediately and cancels delayed clipboard purges.</summary>
        private void ClearClipboardNow()
        {
            CancelClipboardClear();
            ClipboardGuard.ClearNow(this);
        }

        /// <summary>
        /// Performs the server-backed password-health check and posts local notifications when needed.
        /// </summary>
        /// <param name="context">Application/service context.</param>
        internal static async Task PerformCheckAsync(Context context)
        {
            int userId = await StorageHelper.GetUserId();
            if (userId <= 0) return;

            var result = await PasswordCheckService.FetchAsync(userId);
            if (result == null) return;

            NotificationHelper.PostCheckResult(context, result);
        }

    }
}
