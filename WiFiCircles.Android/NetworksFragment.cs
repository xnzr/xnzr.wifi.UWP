using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using System.Collections.ObjectModel;

namespace WiFiCircles
{
    public class NetworksFragment : ListFragment
    {
        public NetworksFragment(ObservableCollection<Data.NetworkInfo> networkInfo, Action<string, string> selectNetworkAction)
        {
            _networkInfo = networkInfo;
            _selectNetworkAction = selectNetworkAction;
        }

        private ObservableCollection<Data.NetworkInfo> _networkInfo;
        private Action<string, string> _selectNetworkAction;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your fragment here
            if (ListAdapter == null)
            {
                ListAdapter = new NetworksListAdapter(Activity, _networkInfo);
            }
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            // Use this to return your custom view for this Fragment
            return inflater.Inflate(Resource.Layout.FragmentLayoutNetworks, container, false);
        }

        public override void OnResume()
        {
            base.OnResume();
            this.ListView.ChoiceMode = ChoiceMode.Single;
        }

        public override void OnListItemClick(ListView l, View v, int position, long id)
        {
            base.OnListItemClick(l, v, position, id);

            var network = _networkInfo[position];
            _selectNetworkAction?.Invoke(network.Ssid, network.Mac);
        }
    }
}