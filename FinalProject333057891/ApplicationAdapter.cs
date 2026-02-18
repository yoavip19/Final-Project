using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Java.Lang;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FinalProject333057891
{
    public class ApplicationAdapter : BaseAdapter<Application>, IFilterable
    {
        private readonly List<Application> originalItems;
        private List<Application> filteredItems;
        private readonly Context context;
        private readonly ApplicationFilter filter;
        private bool enableFiltering;

        public ApplicationAdapter(Context context, List<Application> items, bool enableFiltering = true)
        {
            this.context = context;
            this.enableFiltering = enableFiltering;
            this.originalItems = items ?? new List<Application>();
            this.filteredItems = new List<Application>(this.originalItems);
            this.filter = new ApplicationFilter(this);
        }

        public override Application this[int position] => filteredItems[position];

        public override int Count => filteredItems.Count;

        public override long GetItemId(int position) => position;

        public Filter Filter => filter;

        /// <summary>
        /// Enable or disable filtering dynamically
        /// </summary>
        public void SetFilteringEnabled(bool enabled)
        {
            enableFiltering = enabled;
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            var view = LayoutInflater.From(context).Inflate(Resource.Layout.app_suggestion_item, parent, false);

            var app = filteredItems[position];

            var ivIcon = view.FindViewById<ImageView>(Resource.Id.ivAppIcon);
            var tvName = view.FindViewById<TextView>(Resource.Id.tvAppName);

            tvName.Text = app.AppName ?? "";

            Bitmap icon = null;
            if (!string.IsNullOrEmpty(app.IconBase64))
            {
                try
                {
                    icon = Application.Base64ToBitmap(app.IconBase64);
                }
                catch
                {
                    icon = null;
                }
            }
            ivIcon.SetImageBitmap(icon);

            return view;
        }

        private class ApplicationFilter : Filter
        {
            private readonly ApplicationAdapter adapter;

            public ApplicationFilter(ApplicationAdapter adapter)
            {
                this.adapter = adapter;
            }

            protected override FilterResults PerformFiltering(ICharSequence constraint)
            {
                var results = new FilterResults();

                // If filtering is disabled, return all items
                if (!adapter.enableFiltering)
                {
                    var allItems = new List<Application>(adapter.originalItems);
                    results.Values = new JavaList<Application>(allItems);
                    results.Count = allItems.Count;
                    return results;
                }

                // Normal filtering behavior
                if (constraint == null || constraint.Length() == 0)
                {
                    var list = new List<Application>(adapter.originalItems);
                    results.Values = new JavaList<Application>(list);
                    results.Count = list.Count;
                }
                else
                {
                    string query = constraint.ToString().ToLowerInvariant();
                    var filtered = adapter.originalItems
                        .Where(a => !string.IsNullOrEmpty(a.AppName) && a.AppName.ToLowerInvariant().Contains(query))
                        .ToList();

                    results.Values = new JavaList<Application>(filtered);
                    results.Count = filtered.Count;
                }

                return results;
            }

            protected override void PublishResults(ICharSequence constraint, FilterResults results)
            {
                var values = results?.Values.JavaCast<JavaList<Application>>();

                if (values != null && values.Count > 0)
                {
                    adapter.filteredItems = values.ToList();
                    adapter.NotifyDataSetChanged();
                }
                else
                {
                    // If no results, clear the list or reset
                    adapter.filteredItems = new List<Application>();
                    adapter.NotifyDataSetInvalidated();
                }
            }
        }
    }
}