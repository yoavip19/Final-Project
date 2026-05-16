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

namespace SecurioClient.Activities
{
    /// <summary>Displays the list of vault passwords that fall under a specific risk category (leaked, weak, reused, or old) filtered from the session cache.</summary>
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar")]
    public class RiskDetailActivity : SecuredAppCompatActivity
    {
        /// <summary>Intent extra key for the risk category string ("leaked", "weak", "reused", "old").</summary>
        public const string ExtraRiskCategory = "EXTRA_RISK_CATEGORY";

        // Risk category constants matching the values passed via intent.
        public const string CategoryLeaked = "leaked";
        public const string CategoryWeak   = "weak";
        public const string CategoryReused = "reused";
        public const string CategoryOld    = "old";

        // -- Views ----------------------------------------------
        private ImageView imageViewBack;
        private TextView textViewEmoji;
        private TextView textViewTitle;
        private TextView textViewSubtitle;
        private EditText editTextSearch;
        private RecyclerView recyclerView;
        private LinearLayout layoutEmpty;

        private PasswordBannerAdapter adapter;
        /// <summary>The list of vault items matching the current risk category.</summary>
        private List<VaultItem> riskEntries = new List<VaultItem>();
        private string category;

        // -- Lifecycle ------------------------------------------

        /// <summary>Initializes the activity, loads risk entries from the vault cache, and sets up the UI.</summary>
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

        // -- Setup helpers --------------------------------------

        /// <summary>Finds and assigns view references from the layout.</summary>
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

        /// <summary>Configures the header text and emoji based on the risk category.</summary>
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

        /// <summary>Configures the RecyclerView with the password banner adapter.</summary>
        private void SetupRecyclerView()
        {
            adapter = new PasswordBannerAdapter(riskEntries);
            recyclerView.SetLayoutManager(new LinearLayoutManager(this));
            recyclerView.SetAdapter(adapter);

            // Both the full-banner tap and the kebab icon open the options bottom sheet.
            adapter.ItemClick += (sender, position) => OnBannerActionAt(position);
            adapter.EditClick += (sender, position) => OnBannerActionAt(position);
        }

        /// <summary>Resolves the entry at the given position and opens the options bottom sheet via PasswordEntryActionsHelper.</summary>
        private void OnBannerActionAt(int position)
        {
            var displayed = GetDisplayedEntries();
            if (position >= 0 && position < displayed.Count)
                PasswordEntryActionsHelper.ShowOptionsSheet(
                    this,
                    displayed[position],
                    null,
                    OnEntryDeleted);
        }

        /// <summary>Removes a deleted entry from the list and refreshes the display.</summary>
        private void OnEntryDeleted(VaultItem entry)
        {
            riskEntries.RemoveAll(x => x.Id == entry.Id);
            RefreshList();
        }

        /// <summary>Wires up click and text-change event handlers.</summary>
        private void SetupEventHandlers()
        {
            imageViewBack.Click += (s, e) => Finish();

            editTextSearch.AddTextChangedListener(new SimpleTextWatcher(query =>
            {
                FilterPasswords(query);
            }));
        }

        // -- Activity result ------------------------------------

        /// <summary>Handles the result from the edit-password screen and updates the entry in the list.</summary>
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

                // Reload the risk list from the updated vault cache so entries that
                // are no longer at risk (e.g. a breached password replaced with a
                // strong one) are removed immediately.
                _ = LoadRiskEntriesAsync();
            }
        }

        // -- Data loading ---------------------------------------

        /// <summary>Filters the cached vault to only items that match the current risk category.</summary>
        private async System.Threading.Tasks.Task LoadRiskEntriesAsync()
        {
            RiskCategory riskCategory;
            switch (category)
            {
                case CategoryWeak:    riskCategory = RiskCategory.Weak;    break;
                case CategoryReused:  riskCategory = RiskCategory.Reused;  break;
                case CategoryOld:     riskCategory = RiskCategory.Old;     break;
                default:              riskCategory = RiskCategory.Leaked;  break;
            }

            riskEntries = await WarningsHelper.GetItemsAtRisk(
                SessionHelper.CachedVault,
                SessionHelper.SessionVaultKey,
                riskCategory);

            RefreshList();
        }

        // -- Search / filter helpers ----------------------------

        /// <summary>Applies the search query and refreshes the displayed list.</summary>
        private void FilterPasswords(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                adapter.UpdateData(riskEntries);
            else
                adapter.UpdateData(GetFilteredEntries(query));

            UpdateEmptyState();
        }

        /// <summary>Returns the subset of risk entries whose name or username contains the query.</summary>
        private List<VaultItem> GetFilteredEntries(string query)
        {
            return riskEntries
                .Where(e => (e.AccountName != null && e.AccountName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                         || (e.AccountUsername != null && e.AccountUsername.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0))
                .ToList();
        }

        /// <summary>Returns all entries when no query is active, or the filtered subset otherwise.</summary>
        private List<VaultItem> GetDisplayedEntries()
        {
            string query = editTextSearch.Text?.Trim();
            return string.IsNullOrWhiteSpace(query) ? riskEntries : GetFilteredEntries(query);
        }

        /// <summary>Rebuilds the adapter data set and triggers a layout refresh.</summary>
        private void RefreshList()
        {
            string query = editTextSearch.Text?.Trim();
            FilterPasswords(query);
        }

        /// <summary>Shows or hides the empty-state view based on whether the list has items.</summary>
        private void UpdateEmptyState()
        {
            bool isEmpty = adapter.ItemCount == 0;
            layoutEmpty.Visibility  = isEmpty ? ViewStates.Visible : ViewStates.Gone;
            recyclerView.Visibility = isEmpty ? ViewStates.Gone : ViewStates.Visible;
        }
    }
}
