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
using TestLatency.Video;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Media.MediaProperties;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace TestLatency
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private PeerConnection peerConnection;
        private MediaStreamSource _localVideoSource;
        private VideoBridge _localVideoBridge = new VideoBridge(3);
        private bool _localVideoPlaying = false;
        private object _localVideoLock = new object();

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
            _webcamSource = await DeviceVideoTrackSource.CreateAsync();
            _webcamSource.I420AVideoFrameReady += LocalI420AFrameReady;
        }

        private void App_Suspending(object sender, SuspendingEventArgs e)
        {
            if (peerConnection != null)
            {
                peerConnection.Close();
                peerConnection.Dispose();
                peerConnection = null;
                localVideoPlayerElement.SetMediaPlayer(null);
            }
        }

        private MediaStreamSource CreateI420VideoStreamSource(
            uint width, uint height, int framerate)
        {
            if (width == 0)
            {
                throw new ArgumentException("Invalid zero width for video.", "width");
            }
            if (height == 0)
            {
                throw new ArgumentException("Invalid zero height for video.", "height");
            }
            // Note: IYUV and I420 have same memory layout (though different FOURCC)
            // https://docs.microsoft.com/en-us/windows/desktop/medfound/video-subtype-guids
            var videoProperties = VideoEncodingProperties.CreateUncompressed(
                MediaEncodingSubtypes.Iyuv, width, height);
            var videoStreamDesc = new VideoStreamDescriptor(videoProperties);
            videoStreamDesc.EncodingProperties.FrameRate.Numerator = (uint)framerate;
            videoStreamDesc.EncodingProperties.FrameRate.Denominator = 1;
            // Bitrate in bits per second : framerate * frame pixel size * I420=12bpp
            videoStreamDesc.EncodingProperties.Bitrate = ((uint)framerate * width * height * 12);
            var videoStreamSource = new MediaStreamSource(videoStreamDesc);
            videoStreamSource.BufferTime = TimeSpan.Zero;
            videoStreamSource.SampleRequested += OnMediaStreamSourceRequested;
            videoStreamSource.IsLive = true; // Enables optimizations for live sources
            videoStreamSource.CanSeek = false; // Cannot seek live WebRTC video stream
            return videoStreamSource;
        }

        private void OnMediaStreamSourceRequested(MediaStreamSource sender,
            MediaStreamSourceSampleRequestedEventArgs args)
        {
            VideoBridge videoBridge;
            if (sender == _localVideoSource)
                videoBridge = _localVideoBridge;
            else
                return;
            videoBridge.TryServeVideoFrame(args);
        }

        private void LocalI420AFrameReady(I420AVideoFrame frame)
        {
            lock (_localVideoLock)
            {
                if (!_localVideoPlaying)
                {
                    _localVideoPlaying = true;

                    // Capture the resolution into local variable useable from the lambda below
                    uint width = frame.width;
                    uint height = frame.height;

                    // Defer UI-related work to the main UI thread
                    RunOnMainThread(() =>
                    {
                        // Bridge the local video track with the local media player UI
                        int framerate = 30; // assumed, for lack of an actual value
                        _localVideoSource = CreateI420VideoStreamSource(
                            width, height, framerate);
                        var localVideoPlayer = new MediaPlayer();
                        localVideoPlayer.Source = MediaSource.CreateFromMediaStreamSource(
                            _localVideoSource);
                        localVideoPlayerElement.SetMediaPlayer(localVideoPlayer);
                        localVideoPlayer.Play();
                    });
                }
            }
            // Enqueue the incoming frame into the video bridge; the media player will
            // later dequeue it as soon as it's ready.
            _localVideoBridge.HandleIncomingVideoFrame(frame);
        }

        private void RunOnMainThread(Windows.UI.Core.DispatchedHandler handler)
        {
            if (Dispatcher.HasThreadAccess)
            {
                handler.Invoke();
            }
            else
            {
                // Note: use a discard "_" to silence CS4014 warning
                _ = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, handler);
            }
        }
    }
}
