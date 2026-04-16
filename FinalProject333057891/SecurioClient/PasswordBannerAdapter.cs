using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using System;
using System.Collections.Generic;

namespace SecurioClient
{
    /// <summary>
    /// Adapter that feeds <see cref="PasswordEntry"/> items into the vault RecyclerView.
    /// </summary>
    public class PasswordBannerAdapter : RecyclerView.Adapter
    {
        private List<PasswordEntry> items;

        /// <summary>Raised when the user taps a password banner.</summary>
        public event EventHandler<int> ItemClick;

        /// <summary>Raised when the user taps the copy / action icon on a banner.</summary>
        public event EventHandler<int> CopyClick;

        public PasswordBannerAdapter(List<PasswordEntry> items)
        {
            this.items = items ?? new List<PasswordEntry>();
        }

        public override int ItemCount => items.Count;

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            View view = LayoutInflater.From(parent.Context)
                .Inflate(Resource.Layout.item_password_banner, parent, false);
            return new PasswordBannerViewHolder(view);
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            var vh = (PasswordBannerViewHolder)holder;
            var entry = items[position];

            vh.TextViewIcon.Text = string.IsNullOrEmpty(entry.SiteName)
                ? "?"
                : entry.SiteName.Substring(0, 1).ToUpperInvariant();

            vh.TextViewSiteName.Text = entry.SiteName ?? string.Empty;
            vh.TextViewUsername.Text = entry.Username ?? string.Empty;

            vh.ItemView.Click -= vh.OnItemClick;
            vh.ItemView.Click += vh.OnItemClick;
            vh.ItemClickAction = pos => ItemClick?.Invoke(this, pos);

            vh.ImageViewCopy.Click -= vh.OnCopyClick;
            vh.ImageViewCopy.Click += vh.OnCopyClick;
            vh.CopyClickAction = pos => CopyClick?.Invoke(this, pos);
        }

        /// <summary>
        /// Replaces the full data set and refreshes the list.
        /// </summary>
        public void UpdateData(List<PasswordEntry> newItems)
        {
            items = newItems ?? new List<PasswordEntry>();
            NotifyDataSetChanged();
        }

        // ────────────────────────────────────────────────────────
        //  ViewHolder
        // ────────────────────────────────────────────────────────
        private class PasswordBannerViewHolder : RecyclerView.ViewHolder
        {
            public TextView TextViewIcon { get; }
            public TextView TextViewSiteName { get; }
            public TextView TextViewUsername { get; }
            public ImageView ImageViewCopy { get; }

            public Action<int> ItemClickAction { get; set; }
            public Action<int> CopyClickAction { get; set; }

            public PasswordBannerViewHolder(View itemView) : base(itemView)
            {
                TextViewIcon = itemView.FindViewById<TextView>(Resource.Id.textViewBannerIcon);
                TextViewSiteName = itemView.FindViewById<TextView>(Resource.Id.textViewBannerSiteName);
                TextViewUsername = itemView.FindViewById<TextView>(Resource.Id.textViewBannerUsername);
                ImageViewCopy = itemView.FindViewById<ImageView>(Resource.Id.imageViewBannerCopy);
            }

            public void OnItemClick(object sender, EventArgs e)
            {
                ItemClickAction?.Invoke(AdapterPosition);
            }

            public void OnCopyClick(object sender, EventArgs e)
            {
                CopyClickAction?.Invoke(AdapterPosition);
            }
        }
    }
}
