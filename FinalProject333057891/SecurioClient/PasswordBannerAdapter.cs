using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using SecurioModels.DataTransferObjects;
using System;
using System.Collections.Generic;

namespace SecurioClient
{
    /// <summary>Adapter that feeds VaultItem items into the vault RecyclerView.</summary>
    public class PasswordBannerAdapter : RecyclerView.Adapter
    {
        private List<VaultItem> items;

        /// <summary>Raised when the user taps a password banner.</summary>
        public event EventHandler<int> ItemClick;

        /// <summary>Raised when the user taps the edit icon on a banner.</summary>
        public event EventHandler<int> EditClick;

        /// <summary>Initializes a new instance of PasswordBannerAdapter with the given vault items.</summary>
        public PasswordBannerAdapter(List<VaultItem> items)
        {
            this.items = items ?? new List<VaultItem>();
        }

        /// <summary>Returns the total number of items in the adapter.</summary>
        public override int ItemCount => items.Count;

        /// <summary>Inflates the password banner item layout and returns a new PasswordBannerViewHolder.</summary>
        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            View view = LayoutInflater.From(parent.Context)
                .Inflate(Resource.Layout.item_password_banner, parent, false);
            return new PasswordBannerViewHolder(view);
        }

        /// <summary>Binds the vault item at the given position to the view holder.</summary>
        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            var vh = (PasswordBannerViewHolder)holder;
            var entry = items[position];

            vh.TextViewIcon.Text = string.IsNullOrEmpty(entry.AccountName)
                ? "?"
                : entry.AccountName.Substring(0, 1).ToUpperInvariant();

            vh.TextViewSiteName.Text = entry.AccountName ?? string.Empty;
            vh.TextViewUsername.Text = entry.AccountUsername ?? string.Empty;

            vh.ItemView.Click -= vh.OnItemClick;
            vh.ItemView.Click += vh.OnItemClick;
            vh.ItemClickAction = pos => ItemClick?.Invoke(this, pos);

            vh.ImageViewEdit.Click -= vh.OnEditClick;
            vh.ImageViewEdit.Click += vh.OnEditClick;
            vh.EditClickAction = pos => EditClick?.Invoke(this, pos);
        }

        /// <summary>Replaces the full data set and refreshes the list.</summary>
        public void UpdateData(List<VaultItem> newItems)
        {
            items = newItems ?? new List<VaultItem>();
            NotifyDataSetChanged();
        }

        // --------------------------------------------------------
        //  ViewHolder
        // --------------------------------------------------------
        /// <summary>ViewHolder that holds references to the views inside a single password banner item.</summary>
        private class PasswordBannerViewHolder : RecyclerView.ViewHolder
        {
            public TextView TextViewIcon { get; }
            public TextView TextViewSiteName { get; }
            public TextView TextViewUsername { get; }
            public ImageView ImageViewEdit { get; }

            public Action<int> ItemClickAction { get; set; }
            public Action<int> EditClickAction { get; set; }

            /// <summary>Initializes a new instance of PasswordBannerViewHolder and binds view references.</summary>
            public PasswordBannerViewHolder(View itemView) : base(itemView)
            {
                TextViewIcon = itemView.FindViewById<TextView>(Resource.Id.textViewBannerIcon);
                TextViewSiteName = itemView.FindViewById<TextView>(Resource.Id.textViewBannerSiteName);
                TextViewUsername = itemView.FindViewById<TextView>(Resource.Id.textViewBannerUsername);
                ImageViewEdit = itemView.FindViewById<ImageView>(Resource.Id.imageViewBannerEdit);
            }

            /// <summary>Forwards the item click event to ItemClickAction with the current adapter position.</summary>
            public void OnItemClick(object sender, EventArgs e)
            {
                ItemClickAction?.Invoke(AdapterPosition);
            }

            /// <summary>Forwards the edit icon click event to EditClickAction with the current adapter position.</summary>
            public void OnEditClick(object sender, EventArgs e)
            {
                EditClickAction?.Invoke(AdapterPosition);
            }
        }
    }
}
