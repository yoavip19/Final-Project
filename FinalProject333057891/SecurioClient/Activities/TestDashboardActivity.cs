using Android.App;
using Android.Content;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using SecurioClient.Helpers;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SecurioClient.Activities
{
    /// <summary>
    /// Test-only activity that exposes manual triggers for every scenario that requires
    /// a physical device but cannot be driven by automated unit tests.
    ///
    /// Launch via ADB:
    ///   adb shell am start -n com.companyname.securioclient/.Activities.TestDashboardActivity
    ///
    /// Only compiled when <see cref="TestConfig.IsTestMode"/> is true; the activity
    /// is intentionally excluded from any production build by removing it from the
    /// manifest and setting TestConfig.IsTestMode = false before merging.
    /// </summary>
    [Activity(Label = "Securio Test Dashboard", Theme = "@style/AppTheme.NoActionBar")]
    public class TestDashboardActivity : AppCompatActivity
    {
        private TextView textViewServiceStatus;
        private TextView textViewIntervalInfo;
        private TextView textViewLastResult;
        private Button buttonRunCheckNow;
        private Button buttonSimulateBoot;
        private Button buttonSimulateUpdate;
        private ProgressBar progressBar;

        /// <inheritdoc />
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_test_dashboard);

            textViewServiceStatus = FindViewById<TextView>(Resource.Id.textViewServiceStatus);
            textViewIntervalInfo  = FindViewById<TextView>(Resource.Id.textViewIntervalInfo);
            textViewLastResult    = FindViewById<TextView>(Resource.Id.textViewLastResult);
            buttonRunCheckNow     = FindViewById<Button>(Resource.Id.buttonRunCheckNow);
            buttonSimulateBoot    = FindViewById<Button>(Resource.Id.buttonSimulateBoot);
            buttonSimulateUpdate  = FindViewById<Button>(Resource.Id.buttonSimulateUpdate);
            progressBar           = FindViewById<ProgressBar>(Resource.Id.progressBarTestDashboard);

            buttonRunCheckNow.Click    += async (s, e) => await RunCheckNowAsync();
            buttonSimulateBoot.Click   += (s, e) => SimulateBroadcast(Intent.ActionBootCompleted);
            buttonSimulateUpdate.Click += (s, e) => SimulateBroadcast(Intent.ActionMyPackageReplaced);

            RefreshDisplay();
        }

        /// <inheritdoc />
        protected override void OnResume()
        {
            base.OnResume();
            RefreshDisplay();
        }

        // ────────────────────────────────────────────────────
        //  Display helpers
        // ────────────────────────────────────────────────────

        /// <summary>Refreshes all status fields from current in-process state.</summary>
        private void RefreshDisplay()
        {
            // Service running?
            bool running = IsServiceRunning(typeof(PasswordMonitorService));
            textViewServiceStatus.Text = running
                ? "✅ PasswordMonitorService is RUNNING"
                : "❌ PasswordMonitorService is NOT running";

            // Interval
            long intervalSec = TestConfig.CheckIntervalMs / 1000;
            textViewIntervalInfo.Text =
                $"Check interval: {intervalSec} s  (TestConfig.IsTestMode = {TestConfig.IsTestMode})";

            // Last result
            var result = PasswordMonitorService.LastCheckResult;
            if (result == null)
            {
                textViewLastResult.Text =
                    "No result yet — press Run Check to trigger the first cycle.";
            }
            else
            {
                textViewLastResult.Text =
                    $"BreachedCount   : {result.BreachedCount}\n" +
                    $"OldCount        : {result.OldCount}\n" +
                    $"MasterPasswordOld: {result.MasterPasswordOld}";
            }
        }

        /// <summary>Returns true when the given service class is in the active foreground-service list.</summary>
        private bool IsServiceRunning(Type serviceType)
        {
            var am = (ActivityManager)GetSystemService(ActivityService);
            var services = am.GetRunningServices(50);
            return services?.Any(s =>
                s.Service?.ClassName?.Contains(serviceType.Name, StringComparison.OrdinalIgnoreCase) == true) == true;
        }

        // ────────────────────────────────────────────────────
        //  Button handlers
        // ────────────────────────────────────────────────────

        /// <summary>Manually executes the password-check cycle and refreshes the result display.</summary>
        private async Task RunCheckNowAsync()
        {
            SetBusy(true);
            try
            {
                Log.Debug(TestConfig.LogTag, "TestDashboard — manual RunCheckNow triggered.");
                await PasswordMonitorService.PerformCheckAsync(this);
                RefreshDisplay();
                Toast.MakeText(this, "Check complete — see Last Check Result above.", ToastLength.Short).Show();
            }
            catch (Exception ex)
            {
                Toast.MakeText(this, $"Check failed: {ex.Message}", ToastLength.Long).Show();
            }
            finally
            {
                SetBusy(false);
            }
        }

        /// <summary>
        /// Sends a targeted broadcast to <see cref="BootReceiver"/> so the service-restart
        /// path can be verified without rebooting the device or re-installing the APK.
        /// </summary>
        private void SimulateBroadcast(string action)
        {
            Log.Debug(TestConfig.LogTag, $"TestDashboard — simulating broadcast: {action}");
            var intent = new Intent(action);
            intent.SetClass(this, typeof(BootReceiver));
            SendBroadcast(intent);
            Toast.MakeText(this, $"Broadcast sent: {action}", ToastLength.Short).Show();

            // Give the service a moment to start, then refresh.
            _handler.PostDelayed(() => RunOnUiThread(RefreshDisplay), 1500);
        }

        private readonly Android.OS.Handler _handler = new Android.OS.Handler(Looper.MainLooper);

        private void SetBusy(bool busy)
        {
            progressBar.Visibility       = busy ? ViewStates.Visible : ViewStates.Gone;
            buttonRunCheckNow.Enabled    = !busy;
            buttonSimulateBoot.Enabled   = !busy;
            buttonSimulateUpdate.Enabled = !busy;
        }
    }
}
