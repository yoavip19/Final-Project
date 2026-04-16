using Android.Graphics;
using Android.OS;
using Android.Views;
using Android.Widget;
using System;

namespace SecurioClient
{
    /// <summary>
    /// Fragment that renders the bottom navigation bar with three tabs: Vault, Generator, and Profile.
    /// The host activity can subscribe to <see cref="TabSelected"/> to react to tab changes.
    /// </summary>
    public class BottomNavFragment : AndroidX.Fragment.App.Fragment
    {
        /// <summary>Raised when the user selects a different tab. The string is "vault", "generator", or "profile".</summary>
        public event EventHandler<string> TabSelected;

        private string currentTab = "vault";

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            return inflater.Inflate(Resource.Layout.fragment_bottom_nav, container, false);
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            base.OnViewCreated(view, savedInstanceState);

            var tabVault = view.FindViewById<LinearLayout>(Resource.Id.navTabVault);
            var tabGenerator = view.FindViewById<LinearLayout>(Resource.Id.navTabGenerator);
            var tabProfile = view.FindViewById<LinearLayout>(Resource.Id.navTabProfile);

            tabVault.Click += (s, e) => SelectTab(view, "vault");
            tabGenerator.Click += (s, e) => SelectTab(view, "generator");
            tabProfile.Click += (s, e) => SelectTab(view, "profile");

            // Default selection
            SelectTab(view, currentTab);
        }

        private void SelectTab(View root, string tab)
        {
            currentTab = tab;

            var selectedColor = new Color(Resources.GetColor(Resource.Color.vaultBottomNavSelected));
            var unselectedColor = new Color(Resources.GetColor(Resource.Color.vaultBottomNavUnselected));

            // Vault
            root.FindViewById<ImageView>(Resource.Id.iconNavVault)
                .SetColorFilter(tab == "vault" ? selectedColor : unselectedColor, PorterDuff.Mode.SrcIn);
            root.FindViewById<TextView>(Resource.Id.textNavVault)
                .SetTextColor(tab == "vault" ? selectedColor : unselectedColor);

            // Generator
            root.FindViewById<ImageView>(Resource.Id.iconNavGenerator)
                .SetColorFilter(tab == "generator" ? selectedColor : unselectedColor, PorterDuff.Mode.SrcIn);
            root.FindViewById<TextView>(Resource.Id.textNavGenerator)
                .SetTextColor(tab == "generator" ? selectedColor : unselectedColor);

            // Profile
            root.FindViewById<ImageView>(Resource.Id.iconNavProfile)
                .SetColorFilter(tab == "profile" ? selectedColor : unselectedColor, PorterDuff.Mode.SrcIn);
            root.FindViewById<TextView>(Resource.Id.textNavProfile)
                .SetTextColor(tab == "profile" ? selectedColor : unselectedColor);

            TabSelected?.Invoke(this, tab);
        }
    }
}
