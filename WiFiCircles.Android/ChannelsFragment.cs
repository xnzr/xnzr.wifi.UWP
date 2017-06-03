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
    public class ChannelsFragment : ListFragment
    {
        public ChannelsFragment(ObservableCollection<Data.ChannelInfo> channelsInfo, Action<int> selectChannelAction)
        {
            _channelsInfo = channelsInfo;
            _selectChannelAction = selectChannelAction;
        }

        private ObservableCollection<Data.ChannelInfo> _channelsInfo;
        private Action<int> _selectChannelAction;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your fragment here
            if (ListAdapter == null)
            {
                ListAdapter = new ChannelsListAdapter(Activity, _channelsInfo);
            }
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            // Use this to return your custom view for this Fragment
            return inflater.Inflate(Resource.Layout.FragmentLayoutChannels, container, false);
        }

        public override void OnResume()
        {
            base.OnResume();
            this.ListView.ChoiceMode = ChoiceMode.Single;
        }

        public override void OnListItemClick(ListView l, View v, int position, long id)
        {
            base.OnListItemClick(l, v, position, id);

            var channel = _channelsInfo[position];
            _selectChannelAction?.Invoke(channel.Channel);
        }
    }
}