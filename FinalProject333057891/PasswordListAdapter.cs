using System;
using System.Collections.Generic;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using Android.Graphics;

namespace FinalProject333057891
{
    public class PasswordListAdapter : RecyclerView.Adapter
    {
        private List<PasswordItem> passwordItems;
        
        // Events for button clicks
        public event EventHandler<PasswordItem> ViewPasswordClicked;
        public event EventHandler<PasswordItem> EditPasswordClicked;
        public event EventHandler<PasswordItem> DeletePasswordClicked;

        public PasswordListAdapter(List<PasswordItem> items)
        {
            passwordItems = items ?? new List<PasswordItem>();
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            View itemView = LayoutInflater.From(parent.Context)
                .Inflate(Resource.Layout.password_list_item, parent, false);
            return new PasswordViewHolder(itemView, OnViewClick, OnEditClick, OnDeleteClick);
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            PasswordViewHolder viewHolder = holder as PasswordViewHolder;
            PasswordItem item = passwordItems[position];
            
            viewHolder.Bind(item);
        }

        public override int ItemCount => passwordItems.Count;

        // Handle Send button click
        private void OnViewClick(int position)
        {
            if (position >= 0 && position < passwordItems.Count)
            {
                ViewPasswordClicked?.Invoke(this, passwordItems[position]);
            }
        }

        // Handle Edit button click
        private void OnEditClick(int position)
        {
            if (position >= 0 && position < passwordItems.Count)
            {
                EditPasswordClicked?.Invoke(this, passwordItems[position]);
            }
        }

        private void OnDeleteClick(int position)
        {
            if (position >= 0 && position < passwordItems.Count)
            {
                DeletePasswordClicked?.Invoke(this, passwordItems[position]);
            }
        }

        /// <summary>
        /// Update the adapter's data and refresh the view
        /// </summary>
        public void UpdateData(List<PasswordItem> newItems)
        {
            passwordItems = newItems ?? new List<PasswordItem>();
            NotifyDataSetChanged();
        }

        // ViewHolder class
        public class PasswordViewHolder : RecyclerView.ViewHolder
        {
            private ImageView imgAppIcon;
            private TextView tvAppName;
            private Button btnViewPassword;
            private Button btnEditPassword;
            private Button btnDeletePassword;

            public PasswordViewHolder(View itemView, Action<int> viewClickListener, Action<int> editClickListener, Action<int> deleteClickListener) 
                : base(itemView)
            {
                imgAppIcon = itemView.FindViewById<ImageView>(Resource.Id.imgAppIcon);
                tvAppName = itemView.FindViewById<TextView>(Resource.Id.tvAppName);
                btnViewPassword = itemView.FindViewById<Button>(Resource.Id.btnViewPassword);
                btnEditPassword = itemView.FindViewById<Button>(Resource.Id.btnEditPassword);
                btnDeletePassword = itemView.FindViewById<Button>(Resource.Id.btnDeletePassword);

                btnViewPassword.Click += (sender, e) => viewClickListener(AdapterPosition);
                btnEditPassword.Click += (sender, e) => editClickListener(AdapterPosition);
                btnDeletePassword.Click += (sender, e) => deleteClickListener(AdapterPosition);
            }

            public void Bind(PasswordItem item)
            {
                // Set app name
                tvAppName.Text = item.AppName;

                // Set app icon from Base64
                try
                {
                    Bitmap iconBitmap = item.GetIconBitmap();
                    imgAppIcon.SetImageBitmap(iconBitmap);
                }
                catch (Exception)
                {
                    // If icon loading fails, you could set a default icon here
                    imgAppIcon.SetImageResource(Android.Resource.Drawable.IcMenuGallery);
                }
            }
        }
    }
}