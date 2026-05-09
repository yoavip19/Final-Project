using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using SecurioClient.Helpers;
using System;

namespace SecurioClient.Activities
{
    /// <summary>Read-only activity that displays a single vault entry's details with the password decrypted client-side.</summary>
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar")]
    public class ViewPasswordActivity : AppCompatActivity
    {
        public const string ExtraSiteName = "EXTRA_SITE_NAME";
        public const string ExtraUsername = "EXTRA_USERNAME";
        public const string ExtraNotes = "EXTRA_NOTES";
        public const string ExtraIV = "EXTRA_IV";
        public const string ExtraTag = "EXTRA_TAG";
        public const string ExtraCipherText = "EXTRA_CIPHER_TEXT";
        public const string ExtraLastUpdate = "EXTRA_LAST_UPDATE";

        private ImageView imageViewBack;
        private TextView textViewSiteName;
        private TextView textViewUsername;
        private TextView textViewPassword;
        private TextView textViewNotes;
        private TextView textViewLastChanged;
        private View clipboardNoticeCard;
        private TextView textViewClipboardNotice;
        private ImageView buttonCopyUsername;
        private ImageView buttonTogglePassword;
        private ImageView buttonCopyPassword;

        private string decryptedPassword;
        private bool passwordVisible;

        /// <summary>Initializes the activity, inflates the layout, populates fields, and wires up event handlers.</summary>
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_view_password);

            InitializeViews();
            PopulateFields();
            SetupEventHandlers();
        }

        /// <summary>Finds and assigns all view references from the layout.</summary>
        private void InitializeViews()
        {
            imageViewBack = FindViewById<ImageView>(Resource.Id.imageViewViewBack);
            textViewSiteName = FindViewById<TextView>(Resource.Id.textViewViewSiteName);
            textViewUsername = FindViewById<TextView>(Resource.Id.textViewViewUsername);
            textViewPassword = FindViewById<TextView>(Resource.Id.textViewViewPassword);
            textViewNotes = FindViewById<TextView>(Resource.Id.textViewViewNotes);
            textViewLastChanged = FindViewById<TextView>(Resource.Id.textViewViewLastChanged);
            clipboardNoticeCard = FindViewById<View>(Resource.Id.cardViewClipboardNotice);
            textViewClipboardNotice = FindViewById<TextView>(Resource.Id.textViewClipboardNotice);
            buttonCopyUsername = FindViewById<ImageView>(Resource.Id.buttonCopyUsername);
            buttonTogglePassword = FindViewById<ImageView>(Resource.Id.buttonTogglePassword);
            buttonCopyPassword = FindViewById<ImageView>(Resource.Id.buttonCopyPassword);
        }

        private void PopulateFields()
        {
            string siteName = Intent.GetStringExtra(ExtraSiteName) ?? string.Empty;
            string username = Intent.GetStringExtra(ExtraUsername) ?? string.Empty;
            string notes = Intent.GetStringExtra(ExtraNotes) ?? string.Empty;

            textViewSiteName.Text = siteName;
            textViewUsername.Text = username;
            textViewNotes.Text = string.IsNullOrWhiteSpace(notes)
                ? GetString(Resource.String.view_no_notes)
                : notes;

            long lastUpdateTicks = Intent.GetLongExtra(ExtraLastUpdate, 0L);
            if (lastUpdateTicks > 0)
            {
                var lastUpdate = new DateTime(lastUpdateTicks, DateTimeKind.Utc);
                textViewLastChanged.Text = lastUpdate.ToString("MMM d, yyyy 'at' h:mm tt 'UTC'");
            }
            else
            {
                textViewLastChanged.Text = "—";
            }

            string iv = Intent.GetStringExtra(ExtraIV) ?? string.Empty;
            string tag = Intent.GetStringExtra(ExtraTag) ?? string.Empty;
            string cipherText = Intent.GetStringExtra(ExtraCipherText) ?? string.Empty;

            if (!string.IsNullOrEmpty(iv) && !string.IsNullOrEmpty(tag) && !string.IsNullOrEmpty(cipherText))
            {
                try
                {
                    decryptedPassword = EncryptionHelper.DecryptAesGcm(iv, tag, cipherText, SessionHelper.SessionVaultKey);
                }
                catch
                {
                    decryptedPassword = null;
                    Toast.MakeText(this, GetString(Resource.String.view_decrypt_error), ToastLength.Short).Show();
                }
            }
            else
            {
                decryptedPassword = null;
            }

            if (clipboardNoticeCard != null)
                clipboardNoticeCard.Visibility = ViewStates.Gone;

            passwordVisible = false;
            UpdatePasswordDisplay();
        }

        private void SetupEventHandlers()
        {
            imageViewBack.Click += (s, e) => Finish();

            buttonCopyUsername.Click += (s, e) =>
            {
                CopyToClipboard("username", textViewUsername.Text);
                Toast.MakeText(this, GetString(Resource.String.view_copied_username), ToastLength.Short).Show();
            };

            buttonTogglePassword.Click += (s, e) =>
            {
                passwordVisible = !passwordVisible;
                UpdatePasswordDisplay();
            };

            buttonCopyPassword.Click += (s, e) =>
            {
                if (!string.IsNullOrEmpty(decryptedPassword))
                {
                    ClipboardGuard.CopySensitive(this, decryptedPassword);
                    ShowClipboardNotice(GetString(Resource.String.view_copied_password_notice));
                }
            };
        }

        private void UpdatePasswordDisplay()
        {
            if (string.IsNullOrEmpty(decryptedPassword))
            {
                textViewPassword.Text = "••••••••";
                return;
            }

            textViewPassword.Text = passwordVisible
                ? decryptedPassword
                : new string('•', decryptedPassword.Length);
        }

        private void CopyToClipboard(string label, string text)
        {
            var clipboard = (ClipboardManager)GetSystemService(ClipboardService);
            var clip = ClipData.NewPlainText(label, text);
            clipboard.PrimaryClip = clip;
        }

        /// <summary>Displays an on-page security notice after password copy so that long warnings are fully readable.</summary>
        private void ShowClipboardNotice(string message)
        {
            if (clipboardNoticeCard == null || textViewClipboardNotice == null) return;

            textViewClipboardNotice.Text = message;
            clipboardNoticeCard.Visibility = ViewStates.Visible;
        }
    }
}
