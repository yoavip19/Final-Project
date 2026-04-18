using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using AndroidX.AppCompat.App;
using SecurioClient.Helpers;

namespace SecurioClient
{
    /// <summary>
    /// Displays four password-risk warning cards (Leaked, Weak, Reused, Old)
    /// with the number of affected passwords in each category.
    /// Counters are read from <see cref="SessionHelper.CachedWarnings"/>;
    /// if the cache has been invalidated the counters are recomputed on-the-fly.
    /// </summary>
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar")]
    public class WarningsActivity : AppCompatActivity
    {
        private TextView textViewLeakedCount;
        private TextView textViewWeakCount;
        private TextView textViewReusedCount;
        private TextView textViewOldCount;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_warnings);

            InitializeViews();
            SetupBottomNavFragment(savedInstanceState);
            _ = DisplayWarningsAsync();
        }

        private void InitializeViews()
        {
            textViewLeakedCount = FindViewById<TextView>(Resource.Id.textViewLeakedCount);
            textViewWeakCount   = FindViewById<TextView>(Resource.Id.textViewWeakCount);
            textViewReusedCount = FindViewById<TextView>(Resource.Id.textViewReusedCount);
            textViewOldCount    = FindViewById<TextView>(Resource.Id.textViewOldCount);
        }

        private void SetupBottomNavFragment(Bundle savedInstanceState)
        {
            if (savedInstanceState == null)
            {
                var fragment = BottomNavFragment.NewInstance("warnings");
                fragment.TabSelected += OnBottomNavTabSelected;

                SupportFragmentManager
                    .BeginTransaction()
                    .Replace(Resource.Id.frameBottomNav, fragment)
                    .Commit();
            }
        }

        private void OnBottomNavTabSelected(object sender, string tab)
        {
            if (tab == "vault")
            {
                Finish();
            }
            else if (tab != "warnings")
            {
                Toast.MakeText(this,
                    $"{char.ToUpper(tab[0])}{tab.Substring(1)} coming soon!",
                    ToastLength.Short).Show();
            }
        }

        /// <summary>
        /// Reads the cached warnings or recomputes them if the cache was flushed,
        /// then populates the four counter TextViews.
        /// </summary>
        private async System.Threading.Tasks.Task DisplayWarningsAsync()
        {
            var warnings = SessionHelper.CachedWarnings;

            if (warnings == null)
            {
                // Cache was invalidated (vault changed) — recompute with live HIBP check.
                warnings = await WarningsHelper.ComputeWarningsAsync(
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
