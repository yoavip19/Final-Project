using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using AndroidX.RecyclerView.Widget;
using SecurioClient.Helpers;
using SecurioModels.DataTransferObjects;
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
        private List<VaultItem> allEntries = new List<VaultItem>();

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

            // Both the full-banner tap and the more-options icon tap open the options sheet.
            adapter.ItemClick += (sender, position) => OnBannerActionAt(position);
            adapter.EditClick += (sender, position) => OnBannerActionAt(position);
        }

        /// <summary>
        /// Resolves the entry at <paramref name="position"/> in the currently displayed list
        /// and opens the options bottom sheet for it.
        /// </summary>
        private void OnBannerActionAt(int position)
        {
            var displayed = GetDisplayedEntries();
            if (position >= 0 && position < displayed.Count)
                ShowOptionsSheet(displayed[position]);
        }

        /// <summary>
        /// Displays the <see cref="PasswordOptionsBottomSheet"/> for the given entry
        /// and wires up the View, Edit, and Delete callbacks.
        /// </summary>
        private void ShowOptionsSheet(VaultItem entry)
        {
            var sheet = PasswordOptionsBottomSheet.NewInstance(entry);

            sheet.ViewClicked += (s, e) =>
            {
                Toast.MakeText(this, $"View: {entry.AccountName} — coming soon!", ToastLength.Short).Show();
            };

            sheet.EditClicked += (s, e) =>
            {
                Toast.MakeText(this, $"Edit: {entry.AccountName} — coming soon!", ToastLength.Short).Show();
            };

            sheet.DeleteClicked += (s, e) => ConfirmDelete(entry);

            sheet.Show(SupportFragmentManager, PasswordOptionsBottomSheet.TagName);
        }

        /// <summary>
        /// Shows an <see cref="AlertDialog"/> asking the user to confirm deletion of <paramref name="entry"/>.
        /// Removes the entry and refreshes the list on confirmation.
        /// </summary>
        private void ConfirmDelete(VaultItem entry)
        {
            string message = string.Format(
                GetString(Resource.String.sheet_delete_confirm_message),
                entry.AccountName);

            new AndroidX.AppCompat.App.AlertDialog.Builder(this)
                .SetTitle(Resource.String.sheet_delete_confirm_title)
                .SetMessage(message)
                .SetPositiveButton(Resource.String.sheet_delete_confirm_yes, (s, e) =>
                {
                    allEntries.RemoveAll(x => x.Id == entry.Id);
                    RefreshList();
                    Toast.MakeText(this, Resource.String.sheet_deleted_toast, ToastLength.Short).Show();
                })
                .SetNegativeButton(Resource.String.sheet_delete_confirm_no, (s, e) => { })
                .Show();
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
                allEntries.Add(new VaultItem
                {
                    Id = data.GetIntExtra(AddPasswordActivity.ResultEntryId, 0),
                    AccountName = data.GetStringExtra(AddPasswordActivity.ResultSiteName),
                    AccountUsername = data.GetStringExtra(AddPasswordActivity.ResultUsername),
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
            allEntries = new List<VaultItem>
            {
                new VaultItem { Id = 1, AccountName = "Google",   AccountUsername = "user@gmail.com" },
                new VaultItem { Id = 2, AccountName = "GitHub",   AccountUsername = "devuser" },
                new VaultItem { Id = 3, AccountName = "Facebook", AccountUsername = "john.doe@fb.com" },
                new VaultItem { Id = 4, AccountName = "Twitter",  AccountUsername = "@johndoe" },
                new VaultItem { Id = 5, AccountName = "Netflix",  AccountUsername = "john@example.com" },
                new VaultItem { Id = 6, AccountName = "Amazon",   AccountUsername = "shop@example.com" },
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

        private List<VaultItem> GetFilteredEntries(string query)
        {
            return allEntries
                .Where(e => (e.AccountName != null && e.AccountName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                         || (e.AccountUsername != null && e.AccountUsername.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0))
                .ToList();
        }

        /// <summary>
        /// Returns the list currently shown in the adapter (filtered or full).
        /// </summary>
        private List<VaultItem> GetDisplayedEntries()
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
            VaultEntryCache.Entries = new List<VaultItem>(allEntries);
        }
    }
}
