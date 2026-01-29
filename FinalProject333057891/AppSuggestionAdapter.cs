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
    public class AppSuggestionAdapter : BaseAdapter<AppSuggestion>, IFilterable
    {
        private readonly List<AppSuggestion> originalItems;
        private List<AppSuggestion> filteredItems;
        private readonly Context context;
        private readonly SuggestionFilter filter;

        public AppSuggestionAdapter(Context context, List<AppSuggestion> items)
        {
            this.context = context;
            this.originalItems = items ?? new List<AppSuggestion>();
            this.filteredItems = new List<AppSuggestion>(this.originalItems);
            this.filter = new SuggestionFilter(this);
        }

        public override AppSuggestion this[int position] => filteredItems[position];

        public override int Count => filteredItems.Count;

        public override long GetItemId(int position) => position;

        public Filter Filter => filter;

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            var view = LayoutInflater.From(context).Inflate(Resource.Layout.app_suggestion_item, parent, false);

            var suggestion = filteredItems[position];

            var ivIcon = view.FindViewById<ImageView>(Resource.Id.ivAppIcon);
            var tvName = view.FindViewById<TextView>(Resource.Id.tvAppName);

            tvName.Text = suggestion.AppName ?? "";

            Bitmap icon = null;
            if (!string.IsNullOrEmpty(suggestion.IconBase64))
            {
                try
                {
                    icon = Application.Base64ToBitmap(suggestion.IconBase64);
                }
                catch
                {
                    icon = null;
                }
            }
            ivIcon.SetImageBitmap(icon);

            return view;
        }

        private class SuggestionFilter : Filter
        {
            private readonly AppSuggestionAdapter adapter;

            public SuggestionFilter(AppSuggestionAdapter adapter)
            {
                this.adapter = adapter;
            }

            protected override FilterResults PerformFiltering(ICharSequence constraint)
            {
                var results = new FilterResults();

                if (constraint == null || constraint.Length() == 0)
                {
                    var list = new List<AppSuggestion>(adapter.originalItems);
                    results.Values = new JavaList<AppSuggestion>(list);
                    results.Count = list.Count;
                }
                else
                {
                    string query = constraint.ToString().ToLowerInvariant();
                    var filtered = adapter.originalItems
                        .Where(a => !string.IsNullOrEmpty(a.AppName) && a.AppName.ToLowerInvariant().Contains(query))
                        .ToList();

                    results.Values = new JavaList<AppSuggestion>(filtered);
                    results.Count = filtered.Count;
                }

                return results;
            }

            protected override void PublishResults(ICharSequence constraint, FilterResults results)
            {
                var values = results?.Values.JavaCast<JavaList<AppSuggestion>>();

                if (values != null && values.Count > 0)
                {
                    adapter.filteredItems = values.ToList();
                    adapter.NotifyDataSetChanged();
                }
                else
                {
                    // If no results, clear the list or reset
                    adapter.filteredItems = new List<AppSuggestion>();
                    adapter.NotifyDataSetInvalidated();
                }
            }
        }
    }
}