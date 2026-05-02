using Android.App;
using Android.Content;
using Android.OS;
using Android.Provider;
using Android.Util;

namespace SecurioClient.Helpers
{
    /// <summary>Utility for battery-optimisation and manufacturer-specific autostart onboarding.</summary>
    public static class PowerManagerHelper
    {
        private const string Tag = "PowerManagerHelper";

        /// <summary>Returns true when this app is already exempt from battery optimisations.</summary>
        public static bool IsIgnoringBatteryOptimizations(Context context)
        {
            var pm = context.GetSystemService(Context.PowerService) as PowerManager;
            bool ignoring = pm?.IsIgnoringBatteryOptimizations(context.PackageName) ?? true;
            Log.Info(Tag, $"IsIgnoringBatteryOptimizations: {ignoring}");
            return ignoring;
        }

        /// <summary>
        /// Fires the system dialog that asks the user to exempt this app from battery optimisations.
        /// The calling activity should handle the result in OnActivityResult.
        /// </summary>
        public static void RequestIgnoreBatteryOptimizations(Activity activity, int requestCode)
        {
            Log.Info(Tag, "Launching ACTION_REQUEST_IGNORE_BATTERY_OPTIMIZATIONS dialog");
            var intent = new Intent(Settings.ActionRequestIgnoreBatteryOptimizations);
            intent.SetData(Android.Net.Uri.Parse("package:" + activity.PackageName));
            activity.StartActivityForResult(intent, requestCode);
        }

        /// <summary>
        /// Returns an Intent for the OEM-specific Autostart / Protected-Apps settings screen,
        /// or null when the device brand has no known deep-link (stock Android needs no extra step).
        /// The caller should verify the Intent is resolvable before starting it.
        /// </summary>
        public static Intent GetManufacturerAutostartIntent()
        {
            string brand = (Build.Brand ?? "").ToLowerInvariant();
            Log.Info(Tag, $"Detecting OEM autostart intent for brand: '{Build.Brand}'");

            // Xiaomi / MIUI (Redmi, POCO)
            if (brand.Contains("xiaomi") || brand.Contains("redmi") || brand.Contains("poco"))
            {
                Log.Info(Tag, "Detected Xiaomi/MIUI — returning MIUI AutoStart intent");
                var intent = new Intent();
                intent.SetComponent(new ComponentName(
                    "com.miui.securitycenter",
                    "com.miui.permcenter.autostart.AutoStartManagementActivity"));
                return intent;
            }

            // Huawei / Honor
            if (brand.Contains("huawei") || brand.Contains("honor"))
            {
                Log.Info(Tag, "Detected Huawei/Honor — returning EMUI startup manager intent");
                var intent = new Intent();
                intent.SetComponent(new ComponentName(
                    "com.huawei.systemmanager",
                    "com.huawei.systemmanager.startupmgr.ui.StartupNormalAppListActivity"));
                return intent;
            }

            // Oppo / Realme / OnePlus (ColorOS)
            if (brand.Contains("oppo") || brand.Contains("realme") || brand.Contains("oneplus"))
            {
                Log.Info(Tag, "Detected Oppo/Realme/OnePlus — returning ColorOS permission intent");
                var intent = new Intent();
                intent.SetComponent(new ComponentName(
                    "com.coloros.safecenter",
                    "com.coloros.privacypermissionsentry.PermissionTopActivity"));
                return intent;
            }

            // Vivo / iQOO (FuntouchOS)
            if (brand.Contains("vivo"))
            {
                Log.Info(Tag, "Detected Vivo — returning FuntouchOS background startup intent");
                var intent = new Intent();
                intent.SetComponent(new ComponentName(
                    "com.vivo.permissionmanager",
                    "com.vivo.permissionmanager.activity.BgStartUpManagerActivity"));
                return intent;
            }

            // Samsung (One UI)
            if (brand.Contains("samsung"))
            {
                Log.Info(Tag, "Detected Samsung — returning One UI battery activity intent");
                var intent = new Intent();
                intent.SetComponent(new ComponentName(
                    "com.samsung.android.lool",
                    "com.samsung.android.sm.battery.ui.BatteryActivity"));
                return intent;
            }

            Log.Info(Tag, "Brand not matched — no OEM autostart intent returned (stock Android or unrecognised OEM)");
            return null;
        }
    }
}
