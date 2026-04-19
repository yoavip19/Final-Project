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
    /// <summary>
    /// Displays the list of vault passwords that fall under a specific risk category
    /// (leaked, weak, reused, or old). Receives the category via an Intent extra and
    /// filters <see cref="SessionHelper.CachedVault"/> accordingly.
    /// Reuses the same <see cref="PasswordBannerAdapter"/> and search-bar pattern as
    /// <see cref="VaultActivity"/>. The kebab icon on each banner opens the same
    /// <see cref="PasswordOptionsBottomSheet"/> as the vault via
    /// <see cref="PasswordEntryActionsHelper"/>.
    /// </summary>
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar")]
    public class RiskDetailActivity : AppCompatActivity
    {
        /// <summary>Intent extra key for the risk category string ("leaked", "weak", "reused", "old").</summary>
        public const string ExtraRiskCategory = "EXTRA_RISK_CATEGORY";

        // Risk category constants matching the values passed via intent.
        public const string CategoryLeaked = "leaked";
        public const string CategoryWeak   = "weak";
        public const string CategoryReused = "reused";
        public const string CategoryOld    = "old";

        // ── Views ──────────────────────────────────────────────
        private ImageView imageViewBack;
        private TextView textViewEmoji;
        private TextView textViewTitle;
        private TextView textViewSubtitle;
        private EditText editTextSearch;
        private RecyclerView recyclerView;
        private LinearLayout layoutEmpty;

        private PasswordBannerAdapter adapter;
        private List<VaultItem> riskEntries = new List<VaultItem>();
        private string category;

        // ── Lifecycle ──────────────────────────────────────────

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_risk_detail);

            category = Intent.GetStringExtra(ExtraRiskCategory) ?? CategoryLeaked;

            InitializeViews();
            ConfigureHeader();
            SetupRecyclerView();
            SetupEventHandlers();

            _ = LoadRiskEntriesAsync();
        }

        // ── Setup helpers ──────────────────────────────────────

        private void InitializeViews()
        {
            imageViewBack    = FindViewById<ImageView>(Resource.Id.imageViewRiskBack);
            textViewEmoji    = FindViewById<TextView>(Resource.Id.textViewRiskEmoji);
            textViewTitle    = FindViewById<TextView>(Resource.Id.textViewRiskTitle);
            textViewSubtitle = FindViewById<TextView>(Resource.Id.textViewRiskSubtitle);
            editTextSearch   = FindViewById<EditText>(Resource.Id.editTextRiskSearch);
            recyclerView     = FindViewById<RecyclerView>(Resource.Id.recyclerViewRiskPasswords);
            layoutEmpty      = FindViewById<LinearLayout>(Resource.Id.layoutRiskEmpty);
        }

        /// <summary>
        /// Configures the header text and emoji based on the risk category.
        /// </summary>
        private void ConfigureHeader()
        {
            switch (category)
            {
                case CategoryLeaked:
                    textViewEmoji.Text    = "🔓";
                    textViewTitle.Text    = GetString(Resource.String.warnings_leaked_title);
                    textViewSubtitle.Text = GetString(Resource.String.risk_detail_leaked_subtitle);
                    break;
                case CategoryWeak:
                    textViewEmoji.Text    = "💪";
                    textViewTitle.Text    = GetString(Resource.String.warnings_weak_title);
                    textViewSubtitle.Text = GetString(Resource.String.risk_detail_weak_subtitle);
                    break;
                case CategoryReused:
                    textViewEmoji.Text    = "♻️";
                    textViewTitle.Text    = GetString(Resource.String.warnings_reused_title);
                    textViewSubtitle.Text = GetString(Resource.String.risk_detail_reused_subtitle);
                    break;
                case CategoryOld:
                    textViewEmoji.Text    = "⏳";
                    textViewTitle.Text    = GetString(Resource.String.warnings_old_title);
                    textViewSubtitle.Text = GetString(Resource.String.risk_detail_old_subtitle);
                    break;
            }
        }

        private void SetupRecyclerView()
        {
            adapter = new PasswordBannerAdapter(riskEntries);
            recyclerView.SetLayoutManager(new LinearLayoutManager(this));
            recyclerView.SetAdapter(adapter);

            // Both the full-banner tap and the kebab icon open the options bottom sheet.
            adapter.ItemClick += (sender, position) => OnBannerActionAt(position);
            adapter.EditClick += (sender, position) => OnBannerActionAt(position);
        }

        /// <summary>
        /// Resolves the entry at <paramref name="position"/> and opens the options
        /// bottom sheet via the shared <see cref="PasswordEntryActionsHelper"/>.
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
            riskEntries.RemoveAll(x => x.Id == entry.Id);
            RefreshList();
        }

        private void SetupEventHandlers()
        {
            imageViewBack.Click += (s, e) => Finish();

            editTextSearch.AddTextChangedListener(new SimpleTextWatcher(query =>
            {
                FilterPasswords(query);
            }));
        }

        // ── Activity result ────────────────────────────────────

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            if (resultCode != Result.Ok || data == null)
                return;

            if (requestCode == EditPasswordActivity.RequestCodeEdit)
            {
                int editedId = data.GetIntExtra(EditPasswordActivity.ResultEntryId, 0);
                var existing = riskEntries.FirstOrDefault(e => e.Id == editedId);
                if (existing != null)
                {
                    existing.AccountName     = data.GetStringExtra(EditPasswordActivity.ResultSiteName);
                    existing.AccountUsername = data.GetStringExtra(EditPasswordActivity.ResultUsername);
                    existing.Notes           = data.GetStringExtra(EditPasswordActivity.ResultNotes);
                    existing.IV              = data.GetStringExtra(EditPasswordActivity.ResultIV);
                    existing.Tag             = data.GetStringExtra(EditPasswordActivity.ResultTag);
                    existing.CipherText      = data.GetStringExtra(EditPasswordActivity.ResultCipherText);
                    existing.Sha1Hash        = data.GetStringExtra(EditPasswordActivity.ResultSha1Hash);
                    existing.IsLeaked        = data.GetBooleanExtra(EditPasswordActivity.ResultIsLeaked, false);
                    existing.LastUpdate      = new DateTime(data.GetLongExtra(EditPasswordActivity.ResultLastUpdate, existing.LastUpdate.Ticks));

                    SessionHelper.UpdateVaultItem(existing);
                    SessionHelper.InvalidateWarnings();
                }

                RefreshList();
            }
        }

        // ── Data loading ───────────────────────────────────────

        /// <summary>
        /// Filters the cached vault to only items that match the current risk category.
        /// </summary>
        private async System.Threading.Tasks.Task LoadRiskEntriesAsync()
        {
            riskEntries = await WarningsHelper.GetItemsAtRiskAsync(
                SessionHelper.CachedVault,
                SessionHelper.SessionVaultKey,
                category);

            RefreshList();
        }

        // ── Search / filter helpers ────────────────────────────

        private void FilterPasswords(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                adapter.UpdateData(riskEntries);
            else
                adapter.UpdateData(GetFilteredEntries(query));

            UpdateEmptyState();
        }

        private List<VaultItem> GetFilteredEntries(string query)
        {
            return riskEntries
                .Where(e => (e.AccountName != null && e.AccountName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                         || (e.AccountUsername != null && e.AccountUsername.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0))
                .ToList();
        }

        private List<VaultItem> GetDisplayedEntries()
        {
            string query = editTextSearch.Text?.Trim();
            return string.IsNullOrWhiteSpace(query) ? riskEntries : GetFilteredEntries(query);
        }

        private void RefreshList()
        {
            string query = editTextSearch.Text?.Trim();
            FilterPasswords(query);
        }

        private void UpdateEmptyState()
        {
            bool isEmpty = adapter.ItemCount == 0;
            layoutEmpty.Visibility  = isEmpty ? ViewStates.Visible : ViewStates.Gone;
            recyclerView.Visibility = isEmpty ? ViewStates.Gone : ViewStates.Visible;
        }

        /// <summary>
        /// Pushes the full vault cache into <see cref="VaultEntryCache"/> so that
        /// <see cref="EditPasswordActivity"/> can perform duplicate checking.
        /// </summary>
        private void SyncEntryCache()
        {
            VaultEntryCache.Entries = new List<VaultItem>(SessionHelper.CachedVault ?? new List<VaultItem>());
        }
    }
}

