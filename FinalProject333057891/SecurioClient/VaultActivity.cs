using Android.App;
using Android.Content;
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
        private int nextEntryId = 100;

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

            // Tapping a banner — Edit activity will be wired here in the future.
            adapter.ItemClick += (sender, position) =>
            {
                var displayed = GetDisplayedEntries();
                if (position >= 0 && position < displayed.Count)
                {
                    Toast.MakeText(this, $"Edit: {displayed[position].SiteName} — coming soon!", ToastLength.Short).Show();
                }
            };

            // Tapping the edit icon on a banner — Edit activity will be wired here in the future.
            adapter.EditClick += (sender, position) =>
            {
                var displayed = GetDisplayedEntries();
                if (position >= 0 && position < displayed.Count)
                {
                    Toast.MakeText(this, $"Edit: {displayed[position].SiteName} — coming soon!", ToastLength.Short).Show();
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

            // FAB — open AddPasswordActivity.
            FindViewById(Resource.Id.fabAddPassword).Click += (sender, e) =>
            {
                SyncEntryCache();
                var intent = new Intent(this, typeof(AddPasswordActivity));
                StartActivityForResult(intent, AddPasswordActivity.RequestCodeAdd);
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
        //  Activity result handling
        // ──────────────────────────────────────────

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            if (resultCode != Result.Ok || data == null)
                return;

            if (requestCode == AddPasswordActivity.RequestCodeAdd)
            {
                allEntries.Add(new PasswordEntry
                {
                    Id = nextEntryId++,
                    SiteName = data.GetStringExtra(AddPasswordActivity.ResultSiteName),
                    Username = data.GetStringExtra(AddPasswordActivity.ResultUsername),
                    EncryptedPassword = data.GetStringExtra(AddPasswordActivity.ResultPassword),
                    Url = data.GetStringExtra(AddPasswordActivity.ResultUrl),
                    Notes = data.GetStringExtra(AddPasswordActivity.ResultNotes)
                });

                RefreshList();
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

            RefreshList();
        }

        private void FilterPasswords(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                adapter.UpdateData(allEntries);
            }
            else
            {
                adapter.UpdateData(GetFilteredEntries(query));
            }

            UpdateEmptyState();
        }

        private List<PasswordEntry> GetFilteredEntries(string query)
        {
            return allEntries
                .Where(e => (e.SiteName != null && e.SiteName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                         || (e.Username != null && e.Username.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0))
                .ToList();
        }

        /// <summary>
        /// Returns the list currently shown in the adapter (filtered or full).
        /// </summary>
        private List<PasswordEntry> GetDisplayedEntries()
        {
            string query = editTextVaultSearch.Text?.Trim();
            return string.IsNullOrWhiteSpace(query) ? allEntries : GetFilteredEntries(query);
        }

        private void RefreshList()
        {
            string query = editTextVaultSearch.Text?.Trim();
            FilterPasswords(query);
        }

        private void UpdateEmptyState()
        {
            bool isEmpty = adapter.ItemCount == 0;
            layoutVaultEmpty.Visibility = isEmpty ? ViewStates.Visible : ViewStates.Gone;
            recyclerViewPasswords.Visibility = isEmpty ? ViewStates.Gone : ViewStates.Visible;
        }

        /// <summary>
        /// Pushes the current entry list into the static cache so that
        /// entry activities can perform duplicate checking.
        /// </summary>
        private void SyncEntryCache()
        {
            VaultEntryCache.Entries = new List<PasswordEntry>(allEntries);
        }
    }
}
