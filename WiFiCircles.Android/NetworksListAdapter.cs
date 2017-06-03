using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace WiFiCircles
{
    public sealed class NetworksListAdapter : ArrayAdapter<Data.NetworkInfo>
    {
        public NetworksListAdapter(Context context, ObservableCollection<Data.NetworkInfo> collection)
            : base(context, Resource.Layout.RowLayoutNetworkInfo, collection)
        {
            _networks = collection;
            foreach (Data.NetworkInfo item in _networks)
            {
                item.PropertyChanged -= Item_PropertyChanged;
                item.PropertyChanged += Item_PropertyChanged;
            }
            _networks.CollectionChanged -= Collection_CollectionChanged;
            _networks.CollectionChanged += Collection_CollectionChanged;
        }

        protected override void Dispose(bool disposing)
        {
            foreach (Data.NetworkInfo item in _networks)
            {
                item.PropertyChanged -= Item_PropertyChanged;
            }
            _networks.CollectionChanged -= Collection_CollectionChanged;

            base.Dispose(disposing);
        }

        class ViewHolder : Java.Lang.Object
        {
            public TextView ssid;
            public TextView mac;
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            View rowView = convertView;

            if (rowView == null)
            {
                LayoutInflater inflater = (LayoutInflater)Context.GetSystemService(Context.LayoutInflaterService);
                rowView = inflater.Inflate(Resource.Layout.RowLayoutNetworkInfo, null);
                //Configure view holder
                ViewHolder viewHolder = new ViewHolder();
                viewHolder.ssid = rowView.FindViewById<TextView>(Resource.Id.row_ssid);
                viewHolder.mac = rowView.FindViewById<TextView>(Resource.Id.row_mac);
                viewHolder.mac.SetTextColor(viewHolder.mac.TextColors.WithAlpha(100));
                rowView.Tag = viewHolder;
            }

            //Fill data
            ViewHolder holder = (ViewHolder)rowView.Tag;
            var network = _networks[position];
            holder.ssid.Text = network.Ssid;
            holder.mac.Text = network.Mac;

            return rowView;
        }

        private void Collection_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    //AddAll(e.NewItems);
                    foreach (Data.NetworkInfo item in e.NewItems)
                    {
                        item.PropertyChanged -= Item_PropertyChanged;
                        item.PropertyChanged += Item_PropertyChanged;
                        Add(item);
                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    foreach (Data.NetworkInfo item in e.OldItems)
                    {
                        item.PropertyChanged -= Item_PropertyChanged;
                        Remove(item);
                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                    Clear();
                    break;
            }

            //NotifyDataSetChanged();
        }

        private void Item_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            NotifyDataSetChanged();
        }

        private ObservableCollection<Data.NetworkInfo> _networks;
    }
}