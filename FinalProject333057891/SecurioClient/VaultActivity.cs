using Android.App;
using Android.Content;
using Android.Content.PM;
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
        private const int RequestCodeNotificationPermission = 9001;
        private const string PostNotificationsPermission = "android.permission.POST_NOTIFICATIONS";

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

            // Load entries from the in-memory session cache.
            LoadVaultFromSession();

            // Request notification permission (required at runtime on Android 13+).
            RequestNotificationPermissionIfNeeded();
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
                PasswordEntryActionsHelper.ShowOptionsSheet(
                    this,
                    displayed[position],
                    SyncEntryCache,
                    OnEntryDeleted);
        }

        private void OnEntryDeleted(VaultItem entry)
        {
            allEntries.RemoveAll(x => x.Id == entry.Id);
            RefreshList();
        }

        private void SetupBottomNavFragment(Bundle savedInstanceState)
        {
            // Only add the fragment on fresh creation to avoid duplicates on configuration change.
            if (savedInstanceState == null)
            {
                var fragment = BottomNavFragment.NewInstance("vault");
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
            => BottomNavHelper.Navigate(this, tab, "vault");

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
                var newItem = new VaultItem
                {
                    Id              = data.GetIntExtra(AddPasswordActivity.ResultEntryId, 0),
                    AccountName     = data.GetStringExtra(AddPasswordActivity.ResultSiteName),
                    AccountUsername  = data.GetStringExtra(AddPasswordActivity.ResultUsername),
                    Notes           = data.GetStringExtra(AddPasswordActivity.ResultNotes),
                    IV              = data.GetStringExtra(AddPasswordActivity.ResultIV),
                    Tag             = data.GetStringExtra(AddPasswordActivity.ResultTag),
                    CipherText      = data.GetStringExtra(AddPasswordActivity.ResultCipherText),
                    Sha1Hash        = data.GetStringExtra(AddPasswordActivity.ResultSha1Hash),
                    IsLeaked        = data.GetBooleanExtra(AddPasswordActivity.ResultIsLeaked, false),
                    LastUpdate      = new DateTime(data.GetLongExtra(AddPasswordActivity.ResultLastUpdate, DateTime.UtcNow.Ticks))
                };

                allEntries.Add(newItem);
                SessionHelper.AddVaultItem(newItem);
                SessionHelper.InvalidateWarnings();
                RefreshList();
            }
            else if (requestCode == EditPasswordActivity.RequestCodeEdit)
            {
                int editedId = data.GetIntExtra(EditPasswordActivity.ResultEntryId, 0);
                var existing = allEntries.FirstOrDefault(e => e.Id == editedId);
                if (existing != null)
                {
                    existing.AccountName     = data.GetStringExtra(EditPasswordActivity.ResultSiteName);
                    existing.AccountUsername  = data.GetStringExtra(EditPasswordActivity.ResultUsername);
                    existing.Notes           = data.GetStringExtra(EditPasswordActivity.ResultNotes);
                    existing.IV              = data.GetStringExtra(EditPasswordActivity.ResultIV);
                    existing.Tag             = data.GetStringExtra(EditPasswordActivity.ResultTag);
                    existing.CipherText      = data.GetStringExtra(EditPasswordActivity.ResultCipherText);
                    existing.Sha1Hash        = data.GetStringExtra(EditPasswordActivity.ResultSha1Hash);
                    existing.IsLeaked        = data.GetBooleanExtra(EditPasswordActivity.ResultIsLeaked, false);
                    existing.LastUpdate      = new DateTime(data.GetLongExtra(EditPasswordActivity.ResultLastUpdate, existing.LastUpdate.Ticks));

                    long lastUpdateTicks = data.GetLongExtra(EditPasswordActivity.ResultLastUpdate, 0L);
                    if (lastUpdateTicks > 0)
                        existing.LastUpdate = new DateTime(lastUpdateTicks, DateTimeKind.Utc);

                    SessionHelper.UpdateVaultItem(existing);
                    SessionHelper.InvalidateWarnings();
                }

                RefreshList();
            }
        }

        // ──────────────────────────────────────────
        //  Data helpers
        // ──────────────────────────────────────────

        /// <summary>
        /// Loads the vault entries from the in-memory session cache into the local list
        /// and refreshes the RecyclerView.
        /// </summary>
        private void LoadVaultFromSession()
        {
            allEntries = new List<VaultItem>(SessionHelper.CachedVault ?? new List<VaultItem>());
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

        // Requests the POST_NOTIFICATIONS runtime permission on Android 13+ (API 33).
        // Without this, notifications posted by the background worker are silently dropped.
        private void RequestNotificationPermissionIfNeeded()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
            {
                if (CheckSelfPermission(PostNotificationsPermission) != Permission.Granted)
                {
                    RequestPermissions(new[] { PostNotificationsPermission }, RequestCodeNotificationPermission);
                }
            }
        }

    }
}
