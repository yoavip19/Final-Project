using Android.OS;
using Android.Views;
using Android.Widget;
using Google.Android.Material.BottomSheet;
using SecurioModels.DataTransferObjects;
using System;

namespace SecurioClient
{
    /// <summary>
    /// BottomSheetDialogFragment that presents View, Edit, and Delete options
    /// for a single <see cref="VaultItem"/> password entry.
    /// </summary>
    public class PasswordOptionsBottomSheet : BottomSheetDialogFragment
    {
        /// <summary>Tag used when showing the fragment via the support fragment manager.</summary>
        public const string TagName = "PasswordOptions";

        private VaultItem _entry;

        /// <summary>Raised when the user taps the View option.</summary>
        public event EventHandler ViewClicked;

        /// <summary>Raised when the user taps the Edit option.</summary>
        public event EventHandler EditClicked;

        /// <summary>Raised when the user taps the Delete option.</summary>
        public event EventHandler DeleteClicked;

        /// <summary>
        /// Creates a new instance pre-loaded with the given vault entry.
        /// </summary>
        public static PasswordOptionsBottomSheet NewInstance(VaultItem entry)
        {
            return new PasswordOptionsBottomSheet { _entry = entry };
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.bottom_sheet_password_options, container, false);

            // Populate header
            string accountName = _entry?.AccountName ?? string.Empty;
            view.FindViewById<TextView>(Resource.Id.textViewSheetTitle).Text = accountName;
            view.FindViewById<TextView>(Resource.Id.textViewSheetUsername).Text = _entry?.AccountUsername ?? string.Empty;
            view.FindViewById<TextView>(Resource.Id.textViewSheetIcon).Text =
                string.IsNullOrWhiteSpace(accountName) ? "?" : accountName.Substring(0, 1).ToUpperInvariant();

            // View option
            view.FindViewById(Resource.Id.layoutOptionView).Click += (s, e) =>
            {
                Dismiss();
                ViewClicked?.Invoke(this, EventArgs.Empty);
            };

            // Edit option
            view.FindViewById(Resource.Id.layoutOptionEdit).Click += (s, e) =>
            {
                Dismiss();
                EditClicked?.Invoke(this, EventArgs.Empty);
            };

            // Delete option
            view.FindViewById(Resource.Id.layoutOptionDelete).Click += (s, e) =>
            {
                Dismiss();
                DeleteClicked?.Invoke(this, EventArgs.Empty);
            };

            return view;
        }
    }
}
