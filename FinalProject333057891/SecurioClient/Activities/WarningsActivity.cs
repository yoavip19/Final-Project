using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using AndroidX.AppCompat.App;
using SecurioClient.Helpers;

namespace SecurioClient.Activities
{
    /// <summary>Displays four password-risk warning cards (Leaked, Weak, Reused, Old) with the number of affected passwords in each category, reading counters from CachedWarnings.</summary>
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar")]
    public class WarningsActivity : SecuredAppCompatActivity
    {
        private TextView textViewLeakedCount;
        private TextView textViewWeakCount;
        private TextView textViewReusedCount;
        private TextView textViewOldCount;

        private TextView buttonViewAllLeaked;
        private TextView buttonViewAllWeak;
        private TextView buttonViewAllReused;
        private TextView buttonViewAllOld;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_warnings);

            InitializeViews();
            SetupBottomNavFragment(savedInstanceState);
            SetupViewAllButtons();
            DisplayWarnings();
        }

        protected override void OnResume()
        {
            base.OnResume();
            DisplayWarnings();
        }

        private void InitializeViews()
        {
            textViewLeakedCount = FindViewById<TextView>(Resource.Id.textViewLeakedCount);
            textViewWeakCount   = FindViewById<TextView>(Resource.Id.textViewWeakCount);
            textViewReusedCount = FindViewById<TextView>(Resource.Id.textViewReusedCount);
            textViewOldCount    = FindViewById<TextView>(Resource.Id.textViewOldCount);

            buttonViewAllLeaked = FindViewById<TextView>(Resource.Id.buttonViewAllLeaked);
            buttonViewAllWeak   = FindViewById<TextView>(Resource.Id.buttonViewAllWeak);
            buttonViewAllReused = FindViewById<TextView>(Resource.Id.buttonViewAllReused);
            buttonViewAllOld    = FindViewById<TextView>(Resource.Id.buttonViewAllOld);
        }

        private void SetupBottomNavFragment(Bundle savedInstanceState)
        {
            var fragment = SupportFragmentManager.FindFragmentById(Resource.Id.frameBottomNav) as BottomNavFragment;

            if (fragment == null)
            {
                fragment = BottomNavFragment.NewInstance("warnings");

                SupportFragmentManager
                    .BeginTransaction()
                    .Replace(Resource.Id.frameBottomNav, fragment)
                    .Commit();
            }

            fragment.TabSelected -= OnBottomNavTabSelected;
            fragment.TabSelected += OnBottomNavTabSelected;
        }

        private void SetupViewAllButtons()
        {
            buttonViewAllLeaked.Click += (s, e) => OpenRiskDetail(RiskDetailActivity.CategoryLeaked);
            buttonViewAllWeak.Click   += (s, e) => OpenRiskDetail(RiskDetailActivity.CategoryWeak);
            buttonViewAllReused.Click += (s, e) => OpenRiskDetail(RiskDetailActivity.CategoryReused);
            buttonViewAllOld.Click    += (s, e) => OpenRiskDetail(RiskDetailActivity.CategoryOld);
        }

        private void OpenRiskDetail(string category)
        {
            var intent = new Intent(this, typeof(RiskDetailActivity));
            intent.PutExtra(RiskDetailActivity.ExtraRiskCategory, category);
            StartActivity(intent);
        }

        private void OnBottomNavTabSelected(object sender, string tab)
            => BottomNavHelper.Navigate(this, tab, "warnings");

        /// <summary>
        /// Reads the cached warnings or recomputes them synchronously (using stored
        /// IsLeaked flags), then populates the four counter TextViews.
        /// Because this runs synchronously in OnCreate the correct numbers are always
        /// visible before the first UI draw — no "0 → real number" flash.
        /// </summary>
        private void DisplayWarnings()
        {
            var warnings = SessionHelper.CachedWarnings;

            if (warnings == null)
            {
                // Cache was invalidated (vault changed) — recompute synchronously using
                // stored flags (no network calls), so the correct numbers are always
                // visible before the first UI draw.
                warnings = WarningsHelper.ComputeWarningsSync(
                    SessionHelper.CachedVault,
                    SessionHelper.SessionVaultKey);

                SessionHelper.CachedWarnings = warnings;
            }

            textViewLeakedCount.Text = warnings.LeakedCount.ToString();
            textViewWeakCount.Text   = warnings.WeakCount.ToString();
            textViewReusedCount.Text = warnings.ReusedCount.ToString();
            textViewOldCount.Text    = warnings.OldCount.ToString();
        }
    }
}
