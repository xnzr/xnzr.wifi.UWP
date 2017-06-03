using System;
using Android.Content;
using Android.Hardware.Usb;
using Android.Util;

namespace WiFiCircles
{
	public class USBBroadcastReceiver : BroadcastReceiver
	{
		public USBBroadcastReceiver(USBCommunicator communicator)
		{
			_communicator = communicator;
		}

		USBCommunicator _communicator;


		public override void OnReceive (Context context, Intent intent)
		{
			string action = intent.Action;
			if (USBCommunicator.ACTION_USB_PERMISSION.Equals(action))
			{
				lock (this)
				{
					UsbDevice device = (UsbDevice)intent.GetParcelableExtra(UsbManager.ExtraDevice);

					if (intent.GetBooleanExtra(UsbManager.ExtraPermissionGranted, false))
					{
						if (device != null)
						{
							//call method to set up accessory communication
							if (_communicator.UsbDevice.DeviceId == device.DeviceId)
							{
								_communicator.Start();
							}
						}
					} else
					{
						Log.Debug("CDC", "permission denied for accessory " + device.DeviceName);
					}
				}
			}
			else if (UsbManager.ActionUsbDeviceAttached.Equals(action))
			{
				UsbDevice device = (UsbDevice)intent.GetParcelableExtra(UsbManager.ExtraDevice);
				if (device != null)
				{
					_communicator.Connect();
					// call your method that cleans up and closes communication with the accessory
				}
			}
			else if (UsbManager.ActionUsbDeviceDetached.Equals(action))
			{
				UsbDevice device = (UsbDevice)intent.GetParcelableExtra(UsbManager.ExtraDevice);
				if (device != null)
				{
					_communicator.Stop();
					// call your method that cleans up and closes communication with the accessory
				}
			}
		}
	}
}

