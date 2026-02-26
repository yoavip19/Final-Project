using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using System;

namespace FinalProject333057891
{
    [Activity(Label = "LinkCheckerActivity")]
    public class LinkCheckerActivity : BaseActivity
    {
        #region Properties
        EditText etLink;
        Button btnCheckLink;
        TextView tvResult;
        ProgressBar pbLoading;
        #endregion

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.link_checker_layout);

            #region FindViewById
            etLink = FindViewById<EditText>(Resource.Id.etLink);
            btnCheckLink = FindViewById<Button>(Resource.Id.btnCheckLink);
            tvResult = FindViewById<TextView>(Resource.Id.tvResult);
            pbLoading = FindViewById<ProgressBar>(Resource.Id.pbLoading);

            SignedMenuFragment fragment = new SignedMenuFragment();
            SupportFragmentManager.BeginTransaction()
                .Replace(Resource.Id.flLinkCheckerMenuContainer, fragment)
                .Commit();
            #endregion

            btnCheckLink.Click += BtnCheckLink_Click;
        }

        private async void BtnCheckLink_Click(object sender, EventArgs e)
        {
            string url = etLink.Text?.Trim();

            if (string.IsNullOrEmpty(url))
            {
                tvResult.Text = "Please enter a link.";
                tvResult.SetTextColor(Android.Graphics.Color.ParseColor("#FF5252"));
                tvResult.Visibility = ViewStates.Visible;
                return;
            }

            // Add scheme if missing so the API accepts the URL
            if (!url.StartsWith("http://") && !url.StartsWith("https://"))
                url = "https://" + url;

            // Show loading state
            btnCheckLink.Enabled = false;
            pbLoading.Visibility = ViewStates.Visible;
            tvResult.Visibility = ViewStates.Gone;

            try
            {
                var (isSafe, threatType) = await PhishingCheckerAPI.CheckUrlAsync(url);

                if (isSafe)
                {
                    tvResult.Text = "✔ This link appears to be safe.";
                    tvResult.SetTextColor(Android.Graphics.Color.ParseColor("#388E3C"));
                }
                else
                {
                    string friendlyThreat = GetFriendlyThreatName(threatType);
                    tvResult.Text = $"⚠ Danger! This link was flagged as: {friendlyThreat}";
                    tvResult.SetTextColor(Android.Graphics.Color.ParseColor("#FF5252"));
                }
            }
            catch (Exception ex)
            {
                tvResult.Text = "Error checking link. Please verify your connection and try again.";
                tvResult.SetTextColor(Android.Graphics.Color.ParseColor("#FF5252"));
                System.Diagnostics.Debug.WriteLine($"PhishingChecker error: {ex.Message}");
            }
            finally
            {
                pbLoading.Visibility = ViewStates.Gone;
                tvResult.Visibility = ViewStates.Visible;
                btnCheckLink.Enabled = true;
            }
        }

        private string GetFriendlyThreatName(string threatType)
        {
            return threatType switch
            {
                "MALWARE" => "Malware",
                "SOCIAL_ENGINEERING" => "Phishing / Social Engineering",
                "UNWANTED_SOFTWARE" => "Unwanted Software",
                "POTENTIALLY_HARMFUL_APPLICATION" => "Potentially Harmful Application",
                _ => "Unknown Threat"
            };
        }
    }
}