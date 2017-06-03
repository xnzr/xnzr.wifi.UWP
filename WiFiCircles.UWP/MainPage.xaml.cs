using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Devices.Enumeration;
using Windows.Devices.Sensors;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.Media;
using Windows.Media.Capture;
using Windows.System.Display;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace WiFiCircles
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();

            Application.Current.Suspending += Current_Suspending;
            Application.Current.Resuming += Current_Resuming;

            //ViewModel.DataReceived += (s, e) => Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => LogTextBox.Items.Insert(0, e.Info.ToString() + Environment.NewLine));
        }

        private async void Current_Suspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
        {
            if (Frame.CurrentSourcePageType == typeof(MainPage))
            {
                var deferral = e.SuspendingOperation.GetDeferral();

                await CleanupCameraAsync();

                UnregisterOrientationEventHandlers();

                deferral.Complete();
            }
        }

        private async void Current_Resuming(object sender, object e)
        {
            if (Frame.CurrentSourcePageType == typeof(MainPage))
            {
                RegisterOrientationEventHandlers();

                await InitializeCameraAsync();

                ViewModel.Dispatcher = this.Dispatcher;
                ViewModel.Start();
            }
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            RegisterOrientationEventHandlers();

            await InitListOfDevices();

            await InitializeCameraAsync();

            ViewModel.Dispatcher = this.Dispatcher;
            ViewModel.Start();

            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        private Ellipse ellipse = null;

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(WiFiCircles.ViewModel.MainViewModel.Diff) && Convert.ToString(ellipse?.Tag) == ViewModel.Ssid_Id)
            {
                var maxr = Math.Min(PreviewControl.RenderSize.Height, PreviewControl.RenderSize.Width);
                var diameter = maxr / 100 * ViewModel.Diff;
                diameter = Math.Min(/*1000*/maxr, Math.Max(15, diameter));

                ellipse.Width = diameter;
                ellipse.Height = diameter;
                Canvas.SetLeft(ellipse, (PreviewControl.RenderSize.Width - diameter) / 2);
                Canvas.SetTop(ellipse, (PreviewControl.RenderSize.Height - diameter) / 2);
            }
            else if (e.PropertyName == nameof(WiFiCircles.ViewModel.MainViewModel.Level))
            {
                var frac = Math.Abs(-100 - ViewModel.Level) / 100.0d;
                RssiEmptyRow.Height = new GridLength(1 - frac, GridUnitType.Star);
                RssiLevelRow.Height = new GridLength(frac, GridUnitType.Star);
            }
        }

        protected override async void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            ViewModel.PropertyChanged -= ViewModel_PropertyChanged;

            await CleanupCameraAsync();

            UnregisterOrientationEventHandlers();
        }

        #region Camera support
        public List<DeviceInformation> CameraList;
        private DeviceInformation _cameraInfo;

        private async void MediaCapture_Failed(MediaCapture sender, MediaCaptureFailedEventArgs errorEventArgs)
        {
            await CleanupCameraAsync();

            //await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => GetPreviewFrameButton.IsEnabled = _isPreviewing);
        }

        private async void SystemMediaControls_PropertyChanged(SystemMediaTransportControls sender, SystemMediaTransportControlsPropertyChangedEventArgs args)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                // Only handle this event if this page is currently being displayed
                if (args.Property == SystemMediaTransportControlsProperty.SoundLevel && Frame.CurrentSourcePageType == typeof(MainPage))
                {
                    // Check to see if the app is being muted. If so, it is being minimized.
                    // Otherwise if it is not initialized, it is being brought into focus.
                    if (sender.SoundLevel == SoundLevel.Muted)
                    {
                        await CleanupCameraAsync();
                    }
                    else if (!_isInitialized)
                    {
                        await InitializeCameraAsync();
                    }
                }
            });
        }

        private readonly DisplayRequest _displayRequest = new DisplayRequest();
        private readonly SystemMediaTransportControls _systemMediaControls = SystemMediaTransportControls.GetForCurrentView();
        private MediaCapture _mediaCapture;
        private bool _isInitialized = false;
        private bool _isPreviewing = false;
        private bool _mirroringPreview = false;
        private bool _externalCamera = false;

        private async Task InitListOfDevices()
        {
            if (CameraList == null || CameraList.Count == 0)
            {
                CameraList = (await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture)).ToList();
                _cameraInfo = CameraList.FirstOrDefault(x => x.EnclosureLocation != null && x.EnclosureLocation.Panel == Windows.Devices.Enumeration.Panel.Back) ?? CameraList.FirstOrDefault();

                CameraListMenu.Items.Clear();
                foreach (var info in CameraList)
                {
                    var menuItem = new ToggleMenuFlyoutItem { Text = info.Name, Tag = info, IsChecked = info == _cameraInfo, IsEnabled = info != _cameraInfo };
                    menuItem.Click += MenuItem_Click;
                    CameraListMenu.Items.Add(menuItem);
                }
            }
        }

        private async void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            await CleanupCameraAsync();
            _cameraInfo = (sender as MenuFlyoutItem).Tag as DeviceInformation;
            foreach (var item in CameraListMenu.Items)
            {
                (item as ToggleMenuFlyoutItem).IsChecked = item.Tag == _cameraInfo;
                (item as ToggleMenuFlyoutItem).IsEnabled = item.Tag != _cameraInfo;
            }
            await InitializeCameraAsync();
        }

        private async Task InitializeCameraAsync()
        {
            if (_mediaCapture == null)
            {
                // If there is no device mounted on the desired panel, return the first device found
                var cameraDevice = _cameraInfo ?? CameraList.FirstOrDefault();

                if (cameraDevice == null)
                {
                    return;
                }

                // Create MediaCapture and its settings
                _mediaCapture = new MediaCapture();

                // Register for a notification when something goes wrong
                _mediaCapture.Failed += MediaCapture_Failed;

                var settings = new MediaCaptureInitializationSettings { VideoDeviceId = cameraDevice.Id };

                // Initialize MediaCapture
                try
                {
                    await _mediaCapture.InitializeAsync(settings);
                    _isInitialized = true;
                }
                catch (UnauthorizedAccessException)
                {
                }

                // If initialization succeeded, start the preview
                if (_isInitialized)
                {
                    // Figure out where the camera is located
                    if (cameraDevice.EnclosureLocation == null || cameraDevice.EnclosureLocation.Panel == Windows.Devices.Enumeration.Panel.Unknown)
                    {
                        // No information on the location of the camera, assume it's an external camera, not integrated on the device
                        _externalCamera = true;
                    }
                    else
                    {
                        // Camera is fixed on the device
                        _externalCamera = false;

                        // Only mirror the preview if the camera is on the front panel
                        _mirroringPreview = (cameraDevice.EnclosureLocation.Panel == Windows.Devices.Enumeration.Panel.Front);
                    }

                    await StartPreviewAsync();
                }
            }
        }

        private async Task StartPreviewAsync()
        {
            // Prevent the device from sleeping while the preview is running
            _displayRequest.RequestActive();

            // Register to listen for media property changes
            _systemMediaControls.PropertyChanged += SystemMediaControls_PropertyChanged;

            // Set the preview source in the UI and mirror it if necessary
            PreviewControl.Source = _mediaCapture;
            PreviewControl.FlowDirection = _mirroringPreview ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

            // Start the preview
            try
            {
                await _mediaCapture.StartPreviewAsync();
                _isPreviewing = true;
            }
            catch
            { }

            if (_isPreviewing)
            {
                await SetPreviewRotationAsync();
            }
        }

        private async Task SetPreviewRotationAsync()
        {
            // Only need to update the orientation if the camera is mounted on the device
            if (_externalCamera)
                return;

            // Populate orientation variables with the current state
            _displayOrientation = _displayInformation.CurrentOrientation;

            // Calculate which way and how far to rotate the preview
            int rotationDegrees = ConvertDisplayOrientationToDegrees(_displayOrientation);

            // The rotation direction needs to be inverted if the preview is being mirrored
            if (_mirroringPreview)
            {
                rotationDegrees = (360 - rotationDegrees) % 360;
            }

            // Add rotation metadata to the preview stream to make sure the aspect ratio / dimensions match when rendering and getting preview frames
            var props = _mediaCapture.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview);
            props.Properties.Add(RotationKey, rotationDegrees);
            await _mediaCapture.SetEncodingPropertiesAsync(MediaStreamType.VideoPreview, props, null);
        }

        private static readonly Guid RotationKey = new Guid("C380465D-2271-428C-9B83-ECEA3B4A85C1");

        private static int ConvertDisplayOrientationToDegrees(DisplayOrientations orientation)
        {
            switch (orientation)
            {
                case DisplayOrientations.Portrait:
                    return 90;
                case DisplayOrientations.LandscapeFlipped:
                    return 180;
                case DisplayOrientations.PortraitFlipped:
                    return 270;
                case DisplayOrientations.Landscape:
                default:
                    return 0;
            }
        }

        private readonly DisplayInformation _displayInformation = DisplayInformation.GetForCurrentView();
        private DisplayOrientations _displayOrientation = DisplayOrientations.Portrait;

        private readonly SimpleOrientationSensor _orientationSensor = SimpleOrientationSensor.GetDefault();
        private SimpleOrientation _deviceOrientation = SimpleOrientation.NotRotated;
        private CoreApplicationView _logView = null;
        private Frame _logViewFrame = null;

        private void OrientationSensor_OrientationChanged(SimpleOrientationSensor sender, SimpleOrientationSensorOrientationChangedEventArgs args)
        {
            if (args.Orientation != SimpleOrientation.Faceup && args.Orientation != SimpleOrientation.Facedown)
            {
                _deviceOrientation = args.Orientation;
            }
        }

        private void RegisterOrientationEventHandlers()
        {
            // If there is an orientation sensor present on the device, register for notifications
            if (_orientationSensor != null)
            {
                _orientationSensor.OrientationChanged += OrientationSensor_OrientationChanged;
                _deviceOrientation = _orientationSensor.GetCurrentOrientation();
            }

            _displayInformation.OrientationChanged += DisplayInformation_OrientationChanged;
            _displayOrientation = _displayInformation.CurrentOrientation;
        }

        private void UnregisterOrientationEventHandlers()
        {
            if (_orientationSensor != null)
            {
                _orientationSensor.OrientationChanged -= OrientationSensor_OrientationChanged;
            }

            _displayInformation.OrientationChanged -= DisplayInformation_OrientationChanged;
        }

        private async void DisplayInformation_OrientationChanged(DisplayInformation sender, object args)
        {
            _displayOrientation = sender.CurrentOrientation;

            if (_isPreviewing)
            {
                await SetPreviewRotationAsync();
            }
        }

        private async Task StopPreviewAsync()
        {
            _isPreviewing = false;
            await _mediaCapture.StopPreviewAsync();

            // Use the dispatcher because this method is sometimes called from non-UI threads
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                PreviewControl.Source = null;

                // Allow the device to sleep now that the preview is stopped
                _displayRequest.RequestRelease();
            });
        }

        private async Task CleanupCameraAsync()
        {
            if (_isInitialized)
            {
                if (_isPreviewing)
                {
                    // The call to stop the preview is included here for completeness, but can be
                    // safely removed if a call to MediaCapture.Dispose() is being made later,
                    // as the preview will be automatically stopped at that point
                    await StopPreviewAsync();
                }

                _isInitialized = false;
            }

            if (_mediaCapture != null)
            {
                _mediaCapture.Failed -= MediaCapture_Failed;
                _mediaCapture.Dispose();
                _mediaCapture = null;
            }
        }

        private async void CurrentCameraComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            await CleanupCameraAsync();
            await InitializeCameraAsync();
        }
        #endregion Camera support

        private void ListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (ellipse != null)
                ellipse.Tag = null;
            DrawCanvas.Children.Clear();

            var netInfo = e.ClickedItem as Data.NetworkInfo;
            ViewModel.SelectSsid(netInfo.Ssid, netInfo.Mac);
        }

        private void ScanButton_Click(object sender, RoutedEventArgs e)
        {
            if (ellipse != null)
                ellipse.Tag = null;
            DrawCanvas.Children.Clear();

            NetworksList.SelectedItem = null;
            ChannelsList.SelectedItem = null;

            ViewModel.SelectSsid(string.Empty, string.Empty);
        }

        private void ChannelsList_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (ellipse != null)
                ellipse.Tag = null;
            DrawCanvas.Children.Clear();

            ellipse = new Ellipse()
            {
                Stroke = new SolidColorBrush(Colors.White),
                StrokeThickness = 3,
            };
            DrawCanvas.Children.Add(ellipse);

            var chanInfo = e.ClickedItem as Data.ChannelInfo;
            ViewModel.SelectChannel(chanInfo.Channel);

            ellipse.Tag = ViewModel.Ssid_Id;

            ScanButton.IsChecked = false;
            ScanButton.IsEnabled = true;
        }

        private async void ViewLogButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewLogButton.IsChecked.Value)
            {
                _logView = CoreApplication.CreateNewView();
                //_logView.HostedViewClosing += _logView_HostedViewClosing;
                int logViewId = 0;
                await _logView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                    () =>
                    {
                        var appView = ApplicationView.GetForCurrentView();
                        appView.Title = "Log";
                        appView.Consolidated += LogViewPage_Consolidated;
                        logViewId = appView.Id;

                        _logViewFrame = new Frame();
                        Window.Current.Content = _logViewFrame;
                        _logViewFrame.Navigate(typeof(View.LogViewPage));
                        // You have to activate the window in order to show it later.
                        Window.Current.Activate();
                    });
                await ApplicationViewSwitcher.TryShowAsStandaloneAsync(logViewId);
                ViewModel.DataReceived -= ViewModel_DataReceived;
                ViewModel.DataReceived += ViewModel_DataReceived;
            }
            else if (_logView != null)
            {
                await _logView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                    () =>
                    {
                        _logView.CoreWindow.Close();
                        LogViewPage_Consolidated(null, null);
                    });
            }
        }

        private async void LogViewPage_Consolidated(ApplicationView sender, ApplicationViewConsolidatedEventArgs args)
        {
            ApplicationView.GetForCurrentView().Consolidated -= LogViewPage_Consolidated;

            await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                () =>
                {
                    _logView = null;
                    _logViewFrame = null;
                    ViewModel.DataReceived -= ViewModel_DataReceived;
                    ViewLogButton.IsChecked = false;
                });
        }

        private void LogView_Closed(object sender, CoreWindowEventArgs e)
        {
            Window.Current.Closed -= LogView_Closed;
            _logView = null;
            _logViewFrame = null;
            ViewModel.DataReceived -= ViewModel_DataReceived;
            ViewLogButton.IsChecked = false;
        }

        private async void ViewModel_DataReceived(object sender, BeaconInfoEventArgs e)
        {
            if (_logView != null && _logViewFrame != null)
            {
                await _logView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                    () => (_logViewFrame.Content as View.LogViewPage).AddLog(e));
            }
        }
    }
}
