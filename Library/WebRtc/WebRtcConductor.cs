using System;
using Org.WebRtc;
using System.Linq;
using Newtonsoft.Json;
using Windows.UI.Core;
using Windows.Media.Capture;
using System.Threading.Tasks;
using System.Collections.Generic;
using ApolloLensLibrary.Signaller;
using Windows.Media.MediaProperties;
using MediaElement = Windows.UI.Xaml.Controls.MediaElement;


namespace ApolloLensLibrary.WebRtc
{
    /// <summary>
    /// WebRtcConductor Class Implementation.
    /// (1) Initializes Media
    /// (2) Initializes Peer WebRTC Connection
    /// (3) Conducts call
    /// </summary>
    class WebRtcConductor
    {
        #region Variables

        private CoreDispatcher Dispatcher { get; set; }
        private SignallerClient Signaller { get; set; }

        private MediaElement RemoteVideo { get; set; }
        private MediaElement LocalVideo { get; set; }
        private IMediaStreamTrack RemoteVideoTrack { get; set; } // global (client)
        private IMediaStreamTrack LocalVideoTrack { get; set; }
        private IMediaStreamTrack RemoteAudioTrack { get; set; } // global (client)
        private IMediaStreamTrack LocalAudioTrack { get; set; }
        private MediaOptions MediaOpts { get; set; } // global (source & client)

        private CaptureProfile SelectedProfile { get; set; } // global (source)
        private VideoDevice SelectedDevice { get; set; } // global (source)
        private List<RTCPeerConnection> PeerConnections { get; set; } = new List<RTCPeerConnection>();

        #endregion

        #region Initialization

        /// <summary>
        /// Configuration object for initializing a
        /// WebRtcConductor. Contains dependencies for
        /// injection.
        /// 
        /// Core dispatcher & SignallerClient is always
        /// required. The others may be left null
        /// depending on the WebRtcConductor's media options.
        /// </summary>
        public class Config
        {
            /// <summary>
            /// The UI.Xaml element to render remote video to
            /// </summary>
            public MediaElement RemoteVideo { get; set; }

            /// <summary>
            /// The UI.Xaml element to render local video to
            /// </summary>
            public MediaElement LocalVideo { get; set; }

            /// <summary>
            /// The UI core dispatcher.
            /// </summary>
            public CoreDispatcher CoreDispatcher { get; set; }

            /// <summary>
            /// A signaller instance.
            /// </summary>
            public SignallerClient Signaller { get; set; }

        }

        /// <summary>
        /// Asynchronously initialize the object.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public async Task Initialize(Config config)
        {
            if (config == null)
                throw new ArgumentException("Config cannot be null");

            // required elements
            this.Dispatcher = config.CoreDispatcher ?? throw new ArgumentException(
                "Core Dispatcher cannot be null."
            );

            this.Signaller = config.Signaller ?? throw new ArgumentException(
                "Signaller cannot be null."
            );

            this.Signaller.ReceivedOfferExternalHandler += this.ReceivedOffer;
            this.Signaller.ReceivedAnswerExternalHandler += this.ReceivedAnswer;
            this.Signaller.ReceivedIceCandidateExternalHandler += this.ReceivedIceCandidate;

            this.LocalVideo = config.LocalVideo;
            this.RemoteVideo = config.RemoteVideo;

            if (!await this.RequestAccessForMediaCapture())
                throw new UnauthorizedAccessException("Can't access media.");

            await Task.Run(() =>
            {
                var configuration = new WebRtcLibConfiguration();
                var queue = EventQueueMaker.Bind(this.Dispatcher);
                configuration.Queue = queue;
                configuration.AudioCaptureFrameProcessingQueue = EventQueue.GetOrCreateThreadQueueByName("AudioCaptureProcessingQueue");
                configuration.AudioRenderFrameProcessingQueue = EventQueue.GetOrCreateThreadQueueByName("AudioRenderProcessingQueue");
                configuration.VideoFrameProcessingQueue = EventQueue.GetOrCreateThreadQueueByName("VideoFrameProcessingQueue");
                WebRtcLib.Setup(configuration);
            });
        }

        #endregion

        #region MediaInitializer

        /// <summary>
        /// Represents a capture profile.
        /// </summary>
        public class CaptureProfile
        {
            public uint Width { get; set; }
            public uint Height { get; set; }
            public uint FrameRate { get; set; }
            public bool MrcEnabled { get; set; }

            public override string ToString()
            {
                return $"{this.Width} x {this.Height} {this.FrameRate} fps";
            }
        }

        /// <summary>
        /// Represents a video device.
        /// </summary>
        public class VideoDevice
        {
            public string Id { get; set; }
            public string Name { get; set; }

            public override string ToString()
            {
                return this.Name;
            }
        }

        /// <summary>
        /// Immutable struct, used to specify which media to 
        /// load and what to do with it in an IConductor
        /// instance
        /// </summary>
        public struct MediaOptions
        {
            /// <summary>
            /// Internal mutable struct containing the
            /// same fields.
            /// Allows media options to be immutable, but
            /// still be initialized with an object initializer.
            /// </summary>
            public struct Init
            {
                public bool SendVideo { get; set; }
                public bool SendAudio { get; set; }

                public bool ReceiveVideo { get; set; }
                public bool ReceiveAudio { get; set; }

                public bool LocalLoopback { get; set; }
            }

            /// <summary>
            /// Constructor. Takes in mutable MediaOptions.Init 
            /// object.
            /// </summary>
            /// <example>
            /// var options = new MediaOptions(
            ///     new MediaOptions.Init()
            ///        {
            ///            ReceiveVideo = true
            ///        });
            /// </example>
            /// <param name="init"></param>
            public MediaOptions(Init init)
            {
                this.SendAudio = init.SendAudio;
                this.SendVideo = init.SendVideo;
                this.ReceiveAudio = init.ReceiveAudio;
                this.ReceiveVideo = init.ReceiveVideo;
                this.LocalLoopback = init.LocalLoopback;
            }

            public bool SendVideo { get; }
            public bool SendAudio { get; }

            public bool ReceiveVideo { get; }
            public bool ReceiveAudio { get; }

            public bool LocalLoopback { get; }
        }

        /// <summary>
        /// Request Access for media capture.
        /// </summary>
        /// <returns></returns>
        private async Task<bool> RequestAccessForMediaCapture()
        {
            var requester = new MediaCapture();
            var mediaSettings = new MediaCaptureInitializationSettings()
            {
                AudioDeviceId = "",
                VideoDeviceId = "",
                StreamingCaptureMode = StreamingCaptureMode.AudioAndVideo,
                PhotoCaptureSource = PhotoCaptureSource.VideoPreview
            };

            return await requester
                .InitializeAsync(mediaSettings)
                .AsTask()
                .ContinueWith(initResult =>
                {
                    return initResult.Exception == null;
                });
        }

        /// <summary>
        /// Returns all available capture profiles for the
        /// specified device
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        public async Task<IList<CaptureProfile>> GetCaptureProfiles(VideoDevice device)
        {
            var mediaCapture = new MediaCapture();
            var mediaSettings = new MediaCaptureInitializationSettings()
            {
                VideoDeviceId = device.Id
            };

            return await mediaCapture
                .InitializeAsync(mediaSettings)
                .AsTask()
                .ContinueWith(initResult =>
                {
                    if (initResult.Exception != null)
                        return null;

                    return mediaCapture
                        .VideoDeviceController
                        .GetAvailableMediaStreamProperties(MediaStreamType.VideoRecord)
                        .Cast<VideoEncodingProperties>()
                        .Select(prop => new CaptureProfile()
                        {
                            Width = prop.Width,
                            Height = prop.Height,
                            FrameRate = prop.FrameRate.Numerator / prop.FrameRate.Denominator,
                            MrcEnabled = true
                        })
                        .GroupBy(profile => profile.ToString())
                        .Select(grp => grp.First())
                        .OrderBy(profile => profile.Width * profile.Height)
                        .ThenBy(profile => profile.FrameRate)
                        .ToList();
                });
        }

        /// <summary>
        /// Returns all available video capture devices.
        /// </summary>
        /// <returns></returns>
        public async Task<IList<VideoDevice>> GetVideoDevices()
        {
            var devices = await VideoCapturer.GetDevices();
            return devices
                .Select(dev => new VideoDevice()
                {
                    Id = dev.Info.Id,
                    Name = dev.Info.Name
                })
                .ToList();
        }

        /// <summary>
        /// Set the desired video capture device.
        /// I.e., webcam, capture card, usb webcam
        /// </summary>
        /// <param name="mediaDevice"></param>
        void SetSelectedMediaDevice(VideoDevice mediaDevice)
        {
            // video device is global for the source
            this.SelectedDevice = mediaDevice;
        }

        /// <summary>
        /// Set the desired capture profile.
        /// </summary>
        /// <param name="captureProfile"></param>
        void SetSelectedProfile(CaptureProfile captureProfile)
        {
            // capture profile is global for the source
            this.SelectedProfile = captureProfile;
        }

        /// <summary>
        /// Set the desired media options.
        /// Will throw an exception if the conductor
        /// is not configured to support these options.
        /// </summary>
        /// <param name="options"></param>
        void SetMediaOptions(MediaOptions options)
        {
            // media options are global for the source
            if (options.LocalLoopback && this.LocalVideo == null)
                throw new ArgumentException();

            if (options.ReceiveVideo && this.RemoteVideo == null)
                throw new ArgumentException();

            this.MediaOpts = options;
        }

        /// <summary>
        /// Accesses the local video track, specified by
        /// this.selectedDevice and this.selectedProfile.
        /// MUST NOT BE CALLED FROM THE UI THREAD.
        /// </summary>
        /// <param name="factory"></param>
        /// <returns></returns>
        private IMediaStreamTrack GetLocalVideo(IWebRtcFactory factory)
        {
            IReadOnlyList<IConstraint> mandatoryConstraints = new List<IConstraint>() {
                new Constraint("maxWidth", this.SelectedProfile.Width.ToString()),
                new Constraint("minWidth", this.SelectedProfile.Width.ToString()),
                new Constraint("maxHeight", this.SelectedProfile.Height.ToString()),
                new Constraint("minHeight", this.SelectedProfile.Height.ToString()),
                new Constraint("maxFrameRate", this.SelectedProfile.FrameRate.ToString()),
                new Constraint("minFrameRate", this.SelectedProfile.FrameRate.ToString())
            };
            IReadOnlyList<IConstraint> optionalConstraints = new List<IConstraint>();
            var mediaConstraints = new MediaConstraints(mandatoryConstraints, optionalConstraints);

            // this will throw a very unhelpful exception if called from the UI thread
            var videoOptions = new VideoCapturerCreationParameters();
            videoOptions.Id = this.SelectedDevice.Id;
            videoOptions.Name = this.SelectedDevice.Name;
            videoOptions.EnableMrc = false;
            var videoCapturer = VideoCapturer.Create(videoOptions);

            var options = new VideoOptions()
            {
                Factory = factory,
                Capturer = videoCapturer,
                Constraints = mediaConstraints
            };
            var videoTrackSource = VideoTrackSource.Create(options);
            return MediaStreamTrack.CreateVideoTrack("LocalVideo", videoTrackSource);
        }

        /// <summary>
        /// Accesses the local audio track as specified
        /// by the operating system.
        /// MUST NOT BE CALLED FROM THE UI THREAD.
        /// </summary>
        /// <param name="factory"></param>
        /// <returns></returns>
        private IMediaStreamTrack GetLocalAudio(IWebRtcFactory factory)
        {
            var audioOptions = new AudioOptions() 
            {
                Factory = factory
            };

            // this will throw a very unhelpful exception if called from the UI thread
            var audioTrackSource = AudioTrackSource.Create(audioOptions);
            return MediaStreamTrack.CreateAudioTrack("LocalAudio", audioTrackSource);
        }

        #endregion

        #region WebRtcInitializer

        #region HelperFunctions

        /// <summary>
        /// Helper function. Serializes Session Description.
        /// </summary>
        /// <param name="description"></param>
        /// <returns></returns>
        private string SerializeSessionDescription(RTCSessionDescription description)
        {
            var init = new RTCSessionDescriptionInit()
            {
                Sdp = description.Sdp,
                Type = description.SdpType
            };

            return JsonConvert.SerializeObject(init);
        }

        /// <summary>
        /// Helper function. Deserializes Session Description.
        /// </summary>
        /// <param name="raw"></param>
        /// <returns></returns>
        private RTCSessionDescription DeserializeSessionDescription(string raw)
        {
            var init = JsonConvert.DeserializeObject<RTCSessionDescriptionInit>(raw);
            return new RTCSessionDescription(init);
        }

        #endregion

        #region SignallerMessageSenders

        /// <summary>
        /// WebRTC Establishment: Sends offer.
        /// </summary>
        /// <param name="offer"></param>
        /// <returns></returns>
        public async Task SendOffer(RTCSessionDescription offer)
        {
            var message = this.SerializeSessionDescription(offer);
            await this.Signaller.SendMessage("Offer", message);
        }

        /// <summary>
        /// WebRTC Establishment: Sends Answer.
        /// </summary>
        /// <param name="answer"></param>
        /// <returns></returns>
        public async Task SendAnswer(RTCSessionDescription answer)
        {
            var message = this.SerializeSessionDescription(answer);
            await this.Signaller.SendMessage("Answer", message);
        }

        /// <summary>
        /// WebRTC Establishment: Sends Ice Candidate.
        /// </summary>
        /// <param name="iceCandidate"></param>
        /// <returns></returns>
        public async Task SendIceCandidate(RTCIceCandidate iceCandidate)
        {
            var init = iceCandidate.ToJson();
            var message = JsonConvert.SerializeObject(init);
            await this.Signaller.SendMessage("IceCandidate", message);
        }

        /// <summary>
        /// WebRTC Establishment: Sends Shutdown.
        /// </summary>
        /// <returns></returns>
        public async Task SendShutdown()
        {
            await this.Signaller.SendMessage("Shutdown", "");
        }

        #endregion

        #region SignallerMessageReceivedHandlers

        /// <summary>
        /// Message Handler: Received Offer.
        /// Source only.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="offer"></param>
        private async void ReceivedOffer(object sender, SignallerMessage message)
        {
            RTCSessionDescription offer = this.DeserializeSessionDescription(message.Contents);
            this.PeerConnections.Add(await this.BuildPeerConnection(this.MediaOpts));

            await this.PeerConnections[this.PeerConnections.Count - 1].SetRemoteDescription(offer);

            var answer = await this.PeerConnections[this.PeerConnections.Count - 1].CreateAnswer(new RTCAnswerOptions());
            await this.PeerConnections[this.PeerConnections.Count - 1].SetLocalDescription(answer);
            await this.SendAnswer((RTCSessionDescription)answer);

            this.PeerConnections[this.PeerConnections.Count - 1].OnIceCandidate += this.OnIceCandidate;
        }

        /// <summary>
        /// Message Handler: Received Answer.
        /// Client only.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="answer"></param>
        private async void ReceivedAnswer(object sender, SignallerMessage message)
        {
            RTCSessionDescription answer = this.DeserializeSessionDescription(message.Contents);
            await this.PeerConnections[0].SetRemoteDescription(answer);
            this.PeerConnections[0].OnIceCandidate += this.OnIceCandidate;
        }

        /// <summary>
        /// Message Handler: Received Ice Candidate.
        /// Client only.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="candidate"></param>
        private async void ReceivedIceCandidate(object sender, SignallerMessage message)
        {
            var init = JsonConvert.DeserializeObject<RTCIceCandidateInit>(message.Contents);
            RTCIceCandidate candidate = new RTCIceCandidate(init);
            await this.PeerConnections[0].AddIceCandidate(candidate);
        }

        #endregion

        #region PeerConnectionFunctions

        /// <summary>
        /// Peer Connection Handler: On Track Added
        /// </summary>
        /// <param name="ev"></param>
        private void OnTrack(IRTCTrackEvent ev)
        {
            // client only.
            if (ev.Track.Kind == "video")
            {
                this.RemoteVideoTrack = ev.Track;
                if (this.MediaOpts.ReceiveVideo)
                {
                    this.RemoteVideoTrack.Element = MediaElementMaker.Bind(this.RemoteVideo);
                }
            }
            else if (ev.Track.Kind == "audio")
            {
                if (this.MediaOpts.ReceiveAudio)
                {
                    this.RemoteAudioTrack = ev.Track;
                }
            }
        }

        /// <summary>
        /// Peer Connection Handler: On Ice Candidate.
        /// </summary>
        /// <param name="ev"></param>
        private async void OnIceCandidate(IRTCPeerConnectionIceEvent ev)
        {
            await this.SendIceCandidate((RTCIceCandidate)ev.Candidate);
        }

        /// <summary>
        /// WebRTC Establishment: Builds the Peer Connection.
        /// </summary>
        /// <param name="mediaOptions"></param>
        /// <returns></returns>
        private async Task<RTCPeerConnection> BuildPeerConnection(MediaOptions mediaOptions)
        {
            return await Task.Run(() =>
            {

                var factory = new WebRtcFactory(new WebRtcFactoryConfiguration());

                var peerConnection = new RTCPeerConnection(
                    new RTCConfiguration()
                    {
                        Factory = factory,
                        BundlePolicy = RTCBundlePolicy.Balanced,
                        IceTransportPolicy = RTCIceTransportPolicy.All
                    });

                peerConnection.OnTrack += this.OnTrack;

                if ((mediaOptions.SendVideo || mediaOptions.LocalLoopback) && this.LocalVideoTrack == null)
                {
                    if (mediaOptions.SendVideo || mediaOptions.LocalLoopback)
                    {
                        this.LocalVideoTrack = this.GetLocalVideo(factory);
                    }
                }

                if (mediaOptions.SendAudio && this.LocalAudioTrack == null)
                {
                    if (mediaOptions.SendAudio)
                    {
                        this.LocalAudioTrack = this.GetLocalAudio(factory);
                    }
                }

                if (mediaOptions.SendVideo)
                {
                    peerConnection.AddTrack(this.LocalVideoTrack);
                }
                if (mediaOptions.SendAudio)
                {
                    peerConnection.AddTrack(this.LocalAudioTrack);
                }

                if (mediaOptions.LocalLoopback)
                {
                    this.LocalVideoTrack.Element = MediaElementMaker.Bind(this.LocalVideo);
                }

                return peerConnection;
            });
        }

        #endregion

        /// <summary>
        /// Start a "call" based on the current
        /// media options
        /// </summary>
        /// <returns></returns>
        public async Task StartCall()
        {
            // only called by client, so no need to verify identity.
            if (this.PeerConnections.Count != 0)
                throw new Exception("Peer connection already exists.");

            this.PeerConnections.Add(await this.BuildPeerConnection(this.MediaOpts));

            var connectToPeer =
                this.MediaOpts.SendAudio ||
                this.MediaOpts.ReceiveAudio ||
                this.MediaOpts.SendVideo ||
                this.MediaOpts.ReceiveVideo;

            if (connectToPeer)
            {
                var offerOptions = new RTCOfferOptions()
                {
                    OfferToReceiveAudio = this.MediaOpts.ReceiveAudio,
                    OfferToReceiveVideo = this.MediaOpts.ReceiveVideo
                };

                // because this is the client, only one peer connection will exist.
                var offer = await this.PeerConnections[0].CreateOffer(offerOptions);
                await this.PeerConnections[0].SetLocalDescription(offer);
                await this.SendOffer((RTCSessionDescription)offer);
            }
        }

        /// <summary>
        /// TODO: Fix this implementation so that only the peer is disconnected
        /// Shut down a running call, release most 
        /// resources.
        /// </summary>
        /// <returns></returns>
        public Task Shutdown()
        {
            foreach (RTCPeerConnection pc in this.PeerConnections)
            {
                pc.OnIceCandidate -= this.OnIceCandidate;
                pc.OnTrack -= this.OnTrack;
                (pc as IDisposable)?.Dispose();
            }

            this.PeerConnections.Clear();

            if (this.RemoteVideoTrack != null) this.RemoteVideoTrack.Element = null;
            if (this.LocalVideoTrack != null) this.LocalVideoTrack.Element = null;

            (this.RemoteVideoTrack as IDisposable)?.Dispose();
            (this.LocalVideoTrack as IDisposable)?.Dispose();
            (this.RemoteAudioTrack as IDisposable)?.Dispose();
            (this.LocalAudioTrack as IDisposable)?.Dispose();

            this.RemoteVideoTrack = null;
            this.LocalVideoTrack = null;
            this.RemoteAudioTrack = null;
            this.LocalAudioTrack = null;

            GC.Collect();

            return Task.CompletedTask;
        }

        #endregion
    }
}
