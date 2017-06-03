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
using Android.Hardware;

namespace WiFiCircles
{
    public class CameraFragment : Fragment
    {
        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your fragment here
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            // Use this to return your custom view for this Fragment
            return inflater.Inflate(Resource.Layout.FragmentLayoutCamera, container, false);
        }

        public override void OnStart()
        {
            base.OnStart();

            //Adding camera preview
            _camera = GetCameraInstance();
            _cameraPreview = new CameraPreview(Activity, _camera);
            FrameLayout preview = Activity.FindViewById<FrameLayout>(Resource.Id.camera_preview);
            preview.AddView(_cameraPreview);

            //Adding aim drawer
            RelativeLayout camLayout = Activity.FindViewById<RelativeLayout>(Resource.Id.camLayout);
            _aimView = new AimView(camLayout.Context);
            camLayout.AddView(_aimView);

        }

        public override void OnPause()
        {
            base.OnPause();
            if (_camera != null)
                _camera.Release();
            _camera = null;
        }

        public override void OnResume()
        {
            base.OnResume();
            if (_camera == null)
            {
                _camera = GetCameraInstance();
                _cameraPreview.Camera = _camera;
            }
        }

        public void SetLevel(double level)
        {
            if (level > 0)
            {
                var d = _aimView.MaxRadius * 2 / 100f * level;
                d = Math.Min(_aimView.MaxRadius * 2, Math.Max(15, d));
                _aimView.Radius = (float)d / 2;
                _aimView.Invalidate();
            }
            else
            {
                _aimView.Radius = 0;
                _aimView.Invalidate();
            }
        }

        private Camera GetCameraInstance()
        {
            Camera c = null;
            try
            {
                c = Camera.Open(); // attempt to get a Camera instance
            }
            catch (Java.Lang.Exception e)
            {
                e.PrintStackTrace();
                // Camera is not available (in use or does not exist)
            }
            return c; // returns null if camera is unavailable
        }

        private Camera _camera;
        private CameraPreview _cameraPreview;
        private AimView _aimView;
    }
}