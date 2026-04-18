using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using AndroidX.RecyclerView.Widget;
using SecurioClient.Helpers;
using SecurioClient.Helpers.ServerHelpers;
using SecurioModels.DataTransferObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

            // Load entries from the in-memory session cache.
            LoadVaultFromSession();
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
                var intent = new Intent(this, typeof(ViewPasswordActivity));
                intent.PutExtra(ViewPasswordActivity.ExtraSiteName, entry.AccountName);
                intent.PutExtra(ViewPasswordActivity.ExtraUsername, entry.AccountUsername);
                intent.PutExtra(ViewPasswordActivity.ExtraNotes, entry.Notes);
                intent.PutExtra(ViewPasswordActivity.ExtraIV, entry.IV);
                intent.PutExtra(ViewPasswordActivity.ExtraTag, entry.Tag);
                intent.PutExtra(ViewPasswordActivity.ExtraCipherText, entry.CipherText);
                StartActivity(intent);
            };

            sheet.EditClicked += (s, e) =>
            {
                SyncEntryCache();
                var intent = new Intent(this, typeof(EditPasswordActivity));
                intent.PutExtra(EditPasswordActivity.ExtraEntryId, entry.Id);
                intent.PutExtra(EditPasswordActivity.ExtraSiteName, entry.AccountName);
                intent.PutExtra(EditPasswordActivity.ExtraUsername, entry.AccountUsername);
                intent.PutExtra(EditPasswordActivity.ExtraNotes, entry.Notes);
                intent.PutExtra(EditPasswordActivity.ExtraIV, entry.IV);
                intent.PutExtra(EditPasswordActivity.ExtraTag, entry.Tag);
                intent.PutExtra(EditPasswordActivity.ExtraCipherText, entry.CipherText);
                intent.PutExtra(EditPasswordActivity.ExtraSha1Hash, entry.Sha1Hash);
                intent.PutExtra(EditPasswordActivity.ExtraIsLeaked, entry.IsLeaked);
                StartActivityForResult(intent, EditPasswordActivity.RequestCodeEdit);
            };

            sheet.DeleteClicked += (s, e) => ConfirmDelete(entry);

            sheet.Show(SupportFragmentManager, PasswordOptionsBottomSheet.TagName);
        }

        /// <summary>
        /// Shows an <see cref="AlertDialog"/> asking the user to confirm deletion of <paramref name="entry"/>.
        /// On confirmation, removes the entry from the server and then from the local list.
        /// </summary>
        private void ConfirmDelete(VaultItem entry)
        {
            string message = string.Format(
                GetString(Resource.String.sheet_delete_confirm_message),
                entry.AccountName);

            new AndroidX.AppCompat.App.AlertDialog.Builder(this)
                .SetTitle(Resource.String.sheet_delete_confirm_title)
                .SetMessage(message)
                .SetPositiveButton(Resource.String.sheet_delete_confirm_yes, async (s, e) =>
                {
                    await DeleteEntryAsync(entry);
                })
                .SetNegativeButton(Resource.String.sheet_delete_confirm_no, (s, e) => { })
                .Show();
        }

        /// <summary>
        /// Calls the server to delete <paramref name="entry"/>, then removes it from the
        /// local list and session cache. Shows an error toast if the server call fails.
        /// </summary>
        private async Task DeleteEntryAsync(VaultItem entry)
        {
            try
            {
                var vaultService = new VaultService();
                var result = await vaultService.DeleteVaultItemAsync(entry.Id);

                if (result.Success)
                {
                    allEntries.RemoveAll(x => x.Id == entry.Id);
                    SessionHelper.RemoveVaultItem(entry.Id);
                    RefreshList();
                    Toast.MakeText(this, Resource.String.sheet_deleted_toast, ToastLength.Short).Show();
                }
                else
                {
                    Toast.MakeText(this, Resource.String.sheet_delete_error, ToastLength.Long).Show();
                }
            }
            catch (Exception)
            {
                Toast.MakeText(this, Resource.String.sheet_delete_error, ToastLength.Long).Show();
            }
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
                    IsLeaked        = data.GetBooleanExtra(AddPasswordActivity.ResultIsLeaked, false)
                };

                allEntries.Add(newItem);
                SessionHelper.AddVaultItem(newItem);
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

                    SessionHelper.UpdateVaultItem(existing);
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
    }
}
