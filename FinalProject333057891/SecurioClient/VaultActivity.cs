using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using AndroidX.RecyclerView.Widget;
using SecurioClient.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SecurioClient
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar")]
    public class VaultActivity : AppCompatActivity
    {
        private TextView textViewVaultTitle;
        private TextView textViewVaultSubtitle;
        private EditText editTextVaultSearch;
        private RecyclerView recyclerViewPasswords;
        private LinearLayout layoutVaultEmpty;

        private PasswordBannerAdapter adapter;
        private List<PasswordEntry> allEntries = new List<PasswordEntry>();

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_vault);

            InitializeViews();
            SetupRecyclerView();
            SetupBottomNavFragment(savedInstanceState);
            SetupEventHandlers();

            // Load sample data so the UI is not empty on first launch.
            LoadSampleData();
        }

        private void InitializeViews()
        {
            textViewVaultTitle = FindViewById<TextView>(Resource.Id.textViewVaultTitle);
            textViewVaultSubtitle = FindViewById<TextView>(Resource.Id.textViewVaultSubtitle);
            editTextVaultSearch = FindViewById<EditText>(Resource.Id.editTextVaultSearch);
            recyclerViewPasswords = FindViewById<RecyclerView>(Resource.Id.recyclerViewPasswords);
            layoutVaultEmpty = FindViewById<LinearLayout>(Resource.Id.layoutVaultEmpty);
        }

        private void SetupRecyclerView()
        {
            adapter = new PasswordBannerAdapter(allEntries);
            recyclerViewPasswords.SetLayoutManager(new LinearLayoutManager(this));
            recyclerViewPasswords.SetAdapter(adapter);

            adapter.ItemClick += (sender, position) =>
            {
                if (position >= 0 && position < allEntries.Count)
                {
                    Toast.MakeText(this, $"Tapped: {allEntries[position].SiteName}", ToastLength.Short).Show();
                }
            };

            adapter.CopyClick += (sender, position) =>
            {
                if (position >= 0 && position < allEntries.Count)
                {
                    Toast.MakeText(this, $"Edit: {allEntries[position].SiteName}", ToastLength.Short).Show();
                }
            };
        }

        private void SetupBottomNavFragment(Bundle savedInstanceState)
        {
            // Only add the fragment on fresh creation to avoid duplicates on configuration change.
            if (savedInstanceState == null)
            {
                var fragment = new BottomNavFragment();
                fragment.TabSelected += OnBottomNavTabSelected;

                SupportFragmentManager
                    .BeginTransaction()
                    .Replace(Resource.Id.frameBottomNav, fragment)
                    .Commit();
            }
        }

        private void SetupEventHandlers()
        {
            // Live search filtering
            editTextVaultSearch.AddTextChangedListener(new SimpleTextWatcher(query =>
            {
                FilterPasswords(query);
            }));

            // FAB
            FindViewById(Resource.Id.fabAddPassword).Click += (sender, e) =>
            {
                Toast.MakeText(this, GetString(Resource.String.vault_add_password), ToastLength.Short).Show();
            };
        }

        private void OnBottomNavTabSelected(object sender, string tab)
        {
            // Currently only the vault tab is implemented; other tabs show a toast.
            if (tab != "vault")
            {
                Toast.MakeText(this, $"{char.ToUpper(tab[0])}{tab.Substring(1)} coming soon!", ToastLength.Short).Show();
            }
        }

        // ──────────────────────────────────────────
        //  Data helpers
        // ──────────────────────────────────────────

        /// <summary>
        /// Populates the RecyclerView with sample password entries for demonstration.
        /// Replace this with real data loading in the future.
        /// </summary>
        private void LoadSampleData()
        {
            allEntries = new List<PasswordEntry>
            {
                new PasswordEntry { Id = 1, SiteName = "Google",   Username = "user@gmail.com" },
                new PasswordEntry { Id = 2, SiteName = "GitHub",   Username = "devuser" },
                new PasswordEntry { Id = 3, SiteName = "Facebook", Username = "john.doe@fb.com" },
                new PasswordEntry { Id = 4, SiteName = "Twitter",  Username = "@johndoe" },
                new PasswordEntry { Id = 5, SiteName = "Netflix",  Username = "john@example.com" },
                new PasswordEntry { Id = 6, SiteName = "Amazon",   Username = "shop@example.com" },
            };

            adapter.UpdateData(allEntries);
            UpdateEmptyState();
        }

        private void FilterPasswords(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                adapter.UpdateData(allEntries);
            }
            else
            {
                var filtered = allEntries
                    .Where(e => (e.SiteName != null && e.SiteName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                             || (e.Username != null && e.Username.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0))
                    .ToList();
                adapter.UpdateData(filtered);
            }

            UpdateEmptyState();
        }

        private void UpdateEmptyState()
        {
            bool isEmpty = adapter.ItemCount == 0;
            layoutVaultEmpty.Visibility = isEmpty ? ViewStates.Visible : ViewStates.Gone;
            recyclerViewPasswords.Visibility = isEmpty ? ViewStates.Gone : ViewStates.Visible;
        }
    }
}
