using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
// new libraries
using Microsoft.MixedReality.WebRTC;
using System.Diagnostics;
using Windows.Media.Capture;
using Windows.ApplicationModel;
using System.Collections.Generic;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace TestLatency
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private PeerConnection peerConnection;
        public MainPage()
        {
            this.InitializeComponent();
            this.Loaded += OnLoaded;
            Application.Current.Suspending += App_Suspending;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Request access to microphone and camera
            var settings = new MediaCaptureInitializationSettings();
            settings.StreamingCaptureMode = StreamingCaptureMode.AudioAndVideo;
            var capture = new MediaCapture();
            await capture.InitializeAsync(settings);

            // Retrieve a list of available video capture devices (webcams).
            IReadOnlyList<VideoCaptureDevice> deviceList =
            await DeviceVideoTrackSource.GetCaptureDevicesAsync();

            // Get the device list and, for example, print them to the debugger console
            foreach (var device in deviceList)
            {
                // This message will show up in the Output window of Visual Studio
                Debugger.Log(0, "", $"Webcam {device.name} (id: {device.id})\n");
            }

            peerConnection = new PeerConnection();
            var config = new PeerConnectionConfiguration
                {
                    IceServers = new List<IceServer> {
                        new IceServer{ Urls = { "stun:stun.l.google.com:19302" } }
                }
            };
            await peerConnection.InitializeAsync(config);
            Debugger.Log(0, "", "Peer connection initialized successfully.\n");
            DeviceAudioTrackSource _microphoneSource;
            DeviceVideoTrackSource _webcamSource;
            LocalAudioTrack _localAudioTrack;
            LocalVideoTrack _localVideoTrack;
            _webcamSource = await DeviceVideoTrackSource.CreateAsync();
            var videoTrackConfig = new LocalVideoTrackInitConfig
            {
                trackName = "webcam_track"
            };
            _localVideoTrack = LocalVideoTrack.CreateFromSource(_webcamSource, videoTrackConfig);
            _microphoneSource = await DeviceAudioTrackSource.CreateAsync();
            var audioTrackConfig = new LocalAudioTrackInitConfig
            {
                trackName = "microphone_track"
            };
            _localAudioTrack = LocalAudioTrack.CreateFromSource(_microphoneSource, audioTrackConfig);
        }

        private void App_Suspending(object sender, SuspendingEventArgs e)
        {
            if (peerConnection != null)
            {
                peerConnection.Close();
                peerConnection.Dispose();
                peerConnection = null;
            }
        }
    }
}
