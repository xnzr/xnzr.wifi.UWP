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

namespace WiFiCircles
{
    public sealed class ChannelsListAdapter : ArrayAdapter<Data.ChannelInfo>
    {
        public ChannelsListAdapter(Context context, ObservableCollection<Data.ChannelInfo> collection)
            : base(context, Resource.Layout.RowLayoutNetworkInfo, collection)
        {
            _channels = collection;
            foreach (Data.ChannelInfo item in _channels)
            {
                item.PropertyChanged -= Item_PropertyChanged;
                item.PropertyChanged += Item_PropertyChanged;
            }
            _channels.CollectionChanged -= Collection_CollectionChanged;
            _channels.CollectionChanged += Collection_CollectionChanged;
        }

        protected override void Dispose(bool disposing)
        {
            foreach (Data.ChannelInfo item in _channels)
            {
                item.PropertyChanged -= Item_PropertyChanged;
            }
            _channels.CollectionChanged -= Collection_CollectionChanged;

            base.Dispose(disposing);
        }

        class ViewHolder : Java.Lang.Object
        {
            public TextView channel;
            public TextView rssi1;
            public TextView rssi2;
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            View rowView = convertView;

            if (rowView == null)
            {
                LayoutInflater inflater = (LayoutInflater)Context.GetSystemService(Context.LayoutInflaterService);
                rowView = inflater.Inflate(Resource.Layout.RowLayoutChannelInfo, null);
                //Configure view holder
                ViewHolder viewHolder = new ViewHolder();
                viewHolder.channel = rowView.FindViewById<TextView>(Resource.Id.row_channel);
                viewHolder.rssi1 = rowView.FindViewById<TextView>(Resource.Id.row_rssi1);
                viewHolder.rssi2 = rowView.FindViewById<TextView>(Resource.Id.row_rssi2);
                rowView.Tag = viewHolder;
            }

            //Fill data
            ViewHolder holder = (ViewHolder)rowView.Tag;
            var channel = _channels[position];
            holder.channel.Text = $"Channel={channel.Channel}";
            holder.rssi1.Text = $"Rssi1={channel.AvgRssi1}";
            holder.rssi2.Text = $"Rssi2={channel.AvgRssi2}";

            return rowView;
        }

        private void Collection_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    //AddAll(e.NewItems);
                    foreach (Data.ChannelInfo item in e.NewItems)
                    {
                        item.PropertyChanged -= Item_PropertyChanged;
                        item.PropertyChanged += Item_PropertyChanged;
                        Add(item);
                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    foreach (Data.ChannelInfo item in e.OldItems)
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

        private ObservableCollection<Data.ChannelInfo> _channels;
    }
}