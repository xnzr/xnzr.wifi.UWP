using Android.Views;
using Android.Content;
using Android.App;
using Android.Graphics;

namespace WiFiCircles
{
    public class CameraPreview : SurfaceView, ISurfaceHolderCallback
    {
        private ISurfaceHolder _holder;
        private Android.Hardware.Camera _camera;
        private Activity _activity;

        public Android.Hardware.Camera Camera
        {
            get { return _camera; }
            set { _camera = value; }
        }

        public CameraPreview(Activity context, Android.Hardware.Camera camera) : base(context)
        {
            _camera = camera;
            _activity = context;
            _holder = this.Holder;
            _holder.AddCallback(this);
            //_holder.SetType(SurfaceType.PushBuffers);
        }

        #region ISurfaceHolderCallback implementation
        public void SurfaceCreated(ISurfaceHolder holder)
        {
            try
            {
                _camera.SetPreviewDisplay(holder);
                _camera.StartPreview();
            }
            catch
            {
            }
        }

        public void SurfaceChanged(ISurfaceHolder holder, Android.Graphics.Format format, int width, int height)
        {
            if (_holder == null || _camera == null)
                return;

            try
            {
                _camera.StopPreview();
            }
            catch (Java.Lang.Exception e)
            {
                e.PrintStackTrace();
            }

            SetCameraDisplayOrientation();

            try
            {
                _camera.SetPreviewDisplay(holder);
                _camera.StartPreview();
            }
            catch (Java.Lang.Exception e)
            {
                e.PrintStackTrace();
            }
        }

        public void SurfaceDestroyed(ISurfaceHolder holder)
        {
        }
        #endregion ISurfaceHolderCallback implementation

        private void SetCameraDisplayOrientation()
        {
            if (_camera == null)
                return;

            // определ€ем насколько повернут экран от нормального положени€
            SurfaceOrientation rotation = _activity.WindowManager.DefaultDisplay.Rotation;
            int degrees = 0;
            switch (rotation)
            {
                case SurfaceOrientation.Rotation0:
                    degrees = 0;
                    break;
                case SurfaceOrientation.Rotation90:
                    degrees = 90;
                    break;
                case SurfaceOrientation.Rotation180:
                    degrees = 180;
                    break;
                case SurfaceOrientation.Rotation270:
                    degrees = 270;
                    break;
            }

            int result = 0;

            // получаем инфо по камере cameraId
            Android.Hardware.Camera.CameraInfo info = new Android.Hardware.Camera.CameraInfo();
            Android.Hardware.Camera.GetCameraInfo(0, info);

            // задн€€ камера
            if (info.Facing == Android.Hardware.CameraFacing.Back)
            {
                result = ((360 - degrees) + info.Orientation);
            }
            else if (info.Facing == Android.Hardware.CameraFacing.Front)
            {
                // передн€€ камера
                result = ((360 - degrees) - info.Orientation);
                result += 360;
            }
            result = result % 360;
            try
            {
                _camera.SetDisplayOrientation(result);
            }
            catch (Java.Lang.Exception e)
            {
                e.PrintStackTrace();
            }
        }
    }
}
