using Android.Content;
using Android.Widget;
using AndroidX.AppCompat.App;
using SecurioClient.Helpers.ServerHelpers;
using SecurioModels.DataTransferObjects;
using System;
using System.Threading.Tasks;

namespace SecurioClient.Helpers
{
    /// <summary>
    /// Shared helper that encapsulates the View / Edit / Delete bottom-sheet logic
    /// used by both <see cref="VaultActivity"/> and <see cref="RiskDetailActivity"/>.
    /// </summary>
    public static class PasswordEntryActionsHelper
    {
        /// <summary>
        /// Shows the <see cref="PasswordOptionsBottomSheet"/> for <paramref name="entry"/>
        /// and wires up the View, Edit, and Delete callbacks.
        /// </summary>
        /// <param name="activity">The hosting activity (used for navigation and dialogs).</param>
        /// <param name="entry">The vault item the user acted on.</param>
        /// <param name="beforeEdit">
        /// Optional action invoked just before starting <see cref="EditPasswordActivity"/>,
        /// e.g. to sync the duplicate-check entry cache.
        /// </param>
        /// <param name="onDeleted">
        /// Callback invoked (on the UI thread) after the entry has been successfully
        /// deleted so the caller can update its local list.
        /// </param>
        public static void ShowOptionsSheet(
            AppCompatActivity activity,
            VaultItem entry,
            Action beforeEdit,
            Action<VaultItem> onDeleted)
        {
            var sheet = PasswordOptionsBottomSheet.NewInstance(entry);

            sheet.ViewClicked += (s, e) =>
            {
                var intent = new Intent(activity, typeof(ViewPasswordActivity));
                intent.PutExtra(ViewPasswordActivity.ExtraSiteName, entry.AccountName);
                intent.PutExtra(ViewPasswordActivity.ExtraUsername, entry.AccountUsername);
                intent.PutExtra(ViewPasswordActivity.ExtraNotes, entry.Notes);
                intent.PutExtra(ViewPasswordActivity.ExtraIV, entry.IV);
                intent.PutExtra(ViewPasswordActivity.ExtraTag, entry.Tag);
                intent.PutExtra(ViewPasswordActivity.ExtraCipherText, entry.CipherText);
                intent.PutExtra(ViewPasswordActivity.ExtraLastUpdate, entry.LastUpdate.Ticks);
                activity.StartActivity(intent);
            };

            sheet.EditClicked += (s, e) =>
            {
                beforeEdit?.Invoke();
                var intent = new Intent(activity, typeof(EditPasswordActivity));
                intent.PutExtra(EditPasswordActivity.ExtraEntryId, entry.Id);
                intent.PutExtra(EditPasswordActivity.ExtraSiteName, entry.AccountName);
                intent.PutExtra(EditPasswordActivity.ExtraUsername, entry.AccountUsername);
                intent.PutExtra(EditPasswordActivity.ExtraNotes, entry.Notes);
                intent.PutExtra(EditPasswordActivity.ExtraIV, entry.IV);
                intent.PutExtra(EditPasswordActivity.ExtraTag, entry.Tag);
                intent.PutExtra(EditPasswordActivity.ExtraCipherText, entry.CipherText);
                intent.PutExtra(EditPasswordActivity.ExtraSha1Hash, entry.Sha1Hash);
                intent.PutExtra(EditPasswordActivity.ExtraIsLeaked, entry.IsLeaked);
                activity.StartActivityForResult(intent, EditPasswordActivity.RequestCodeEdit);
            };

            sheet.DeleteClicked += (s, e) => ConfirmDelete(activity, entry, onDeleted);

            sheet.Show(activity.SupportFragmentManager, PasswordOptionsBottomSheet.TagName);
        }

        private static void ConfirmDelete(AppCompatActivity activity, VaultItem entry, Action<VaultItem> onDeleted)
        {
            string message = string.Format(
                activity.GetString(Resource.String.sheet_delete_confirm_message),
                entry.AccountName);

            new AlertDialog.Builder(activity)
                .SetTitle(Resource.String.sheet_delete_confirm_title)
                .SetMessage(message)
                .SetPositiveButton(Resource.String.sheet_delete_confirm_yes, async (s, e) =>
                {
                    await DeleteEntryAsync(activity, entry, onDeleted);
                })
                .SetNegativeButton(Resource.String.sheet_delete_confirm_no, (s, e) => { })
                .Show();
        }

        private static async Task DeleteEntryAsync(AppCompatActivity activity, VaultItem entry, Action<VaultItem> onDeleted)
        {
            try
            {
                var vaultService = new VaultService();
                var result = await vaultService.DeleteVaultItemAsync(entry.Id);

                if (result.Success)
                {
                    SessionHelper.RemoveVaultItem(entry.Id);
                    SessionHelper.InvalidateWarnings();
                    onDeleted?.Invoke(entry);
                    Toast.MakeText(activity, Resource.String.sheet_deleted_toast, ToastLength.Short).Show();
                }
                else
                {
                    Toast.MakeText(activity, Resource.String.sheet_delete_error, ToastLength.Long).Show();
                }
            }
            catch (Exception)
            {
                Toast.MakeText(activity, Resource.String.sheet_delete_error, ToastLength.Long).Show();
            }
        }
    }
}
