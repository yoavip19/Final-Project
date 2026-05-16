using Android.Graphics;
using Android.OS;
using Android.Views;
using Android.Widget;
using System;

namespace SecurioClient
{
    /// <summary>Fragment that renders the bottom navigation bar with three tabs: Vault, Warnings, and Profile. The host activity can subscribe to TabSelected to react to tab changes.</summary>
    public class BottomNavFragment : AndroidX.Fragment.App.Fragment
    {
        /// <summary>Raised when the user selects a different tab. The string is "vault", "warnings", or "profile".</summary>
        public event EventHandler<string> TabSelected;

        private const string ArgSelectedTab = "selectedTab";
        private string currentTab = "vault";

        /// <summary>Creates a new BottomNavFragment with the specified tab pre-selected.</summary>
        public static BottomNavFragment NewInstance(string selectedTab)
        {
            var fragment = new BottomNavFragment();
            var args = new Bundle();
            args.PutString(ArgSelectedTab, selectedTab);
            fragment.Arguments = args;
            return fragment;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            return inflater.Inflate(Resource.Layout.fragment_bottom_nav, container, false);
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            base.OnViewCreated(view, savedInstanceState);

            currentTab = Arguments?.GetString(ArgSelectedTab, "vault") ?? "vault";

            var tabVault    = view.FindViewById<LinearLayout>(Resource.Id.navTabVault);
            var tabWarnings = view.FindViewById<LinearLayout>(Resource.Id.navTabWarnings);
            var tabProfile  = view.FindViewById<LinearLayout>(Resource.Id.navTabProfile);

            tabVault.Click    += (s, e) => SelectTab(view, "vault");
            tabWarnings.Click += (s, e) => SelectTab(view, "warnings");
            tabProfile.Click  += (s, e) => SelectTab(view, "profile");

            // Default selection
            SelectTab(view, currentTab);
        }

        private void SelectTab(View root, string tab)
        {
            currentTab = tab;

            var selectedColor   = new Color(Resources.GetColor(Resource.Color.vaultBottomNavSelected));
            var unselectedColor = new Color(Resources.GetColor(Resource.Color.vaultBottomNavUnselected));

            // Vault
            root.FindViewById<ImageView>(Resource.Id.iconNavVault)
                .SetColorFilter(tab == "vault" ? selectedColor : unselectedColor, PorterDuff.Mode.SrcIn);
            root.FindViewById<TextView>(Resource.Id.textNavVault)
                .SetTextColor(tab == "vault" ? selectedColor : unselectedColor);

            // Warnings
            root.FindViewById<ImageView>(Resource.Id.iconNavWarnings)
                .SetColorFilter(tab == "warnings" ? selectedColor : unselectedColor, PorterDuff.Mode.SrcIn);
            root.FindViewById<TextView>(Resource.Id.textNavWarnings)
                .SetTextColor(tab == "warnings" ? selectedColor : unselectedColor);

            // Profile
            root.FindViewById<ImageView>(Resource.Id.iconNavProfile)
                .SetColorFilter(tab == "profile" ? selectedColor : unselectedColor, PorterDuff.Mode.SrcIn);
            root.FindViewById<TextView>(Resource.Id.textNavProfile)
                .SetTextColor(tab == "profile" ? selectedColor : unselectedColor);

            TabSelected?.Invoke(this, tab);
        }
    }
}
