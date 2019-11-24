using ApolloLensLibrary.Signalling;
using ApolloLensLibrary.Utilities;
using ApolloLensLibrary.WebRtc;
using System;
using System.Threading.Tasks;
using WebRtcImplNew;
using Windows.Foundation;
using Windows.Media.SpeechRecognition;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace ApolloLensClient
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private IConductor conductor { get; } = Conductor.Instance;
        private Boolean isConnectedToSource = false;
        private WebsocketSignaller signaller = null;
        private bool isProcessing = false; /* we need a lock/mutex in this trick for users that click too many buttons */
        private ProtocolSignaller<WebRtcSignaller.WebRtcMessage> cursorSignaller = null;
        private const double threshold = 0.48;

        private SpeechRecognizer contSpeechRecognizer;
        private CoreDispatcher dispatcher;

        public MainPage()
        {
            this.DataContext = this;
            this.InitializeComponent();
            

            Logger.WriteMessage += async (message) =>
            {
                await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    this.OutputTextBox.Text += message + Environment.NewLine;
                });
            };

            Application.Current.Suspending += async (s, e) =>
            {
                await this.conductor.UISignaller.SendShutdown();
                await this.conductor.Shutdown();
            };

        }

        protected override async void OnNavigatedTo(NavigationEventArgs args)
        {
            signaller = new WebsocketSignaller("client");
            var protocol = new MessageProtocol<WebRtcSignaller.WebRtcMessage>();
            this.cursorSignaller = new ProtocolSignaller<WebRtcSignaller.WebRtcMessage>
            (
                signaller,
                protocol
            );
            
            // Not implemented for source, but necessary here.
            // EX: signaller connection void before source<=>client connection made.
            // This could happen if the source was disconnected before direct connection.
            signaller.ConnectionEnded += async (s, a) =>
            {
                await this.Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
                {
                    if (!isConnectedToSource)
                    {
                        this.ConnectedOptions.Hide();
                        this.StartupSettings.Show();
                        this.CursorElement.Hide();
                    }
                });
            };

            signaller.ConnectionFailed += (s, a) =>
            {
                this.ConnectedOptions.Hide();
                this.StartupSettings.Show();
                this.CursorElement.Hide();
            };

            dispatcher = CoreWindow.GetForCurrentThread().Dispatcher;
            contSpeechRecognizer = new SpeechRecognizer();
            await contSpeechRecognizer.CompileConstraintsAsync();

            contSpeechRecognizer.HypothesisGenerated += ContSpeechRecognizer_HypothesisGenerated;
            contSpeechRecognizer.ContinuousRecognitionSession.ResultGenerated +=
                ContinuousRecognitionSession_ResultGenerated;


            contSpeechRecognizer.ContinuousRecognitionSession.Completed += ContinuousRecognitionSession_Completed;

            await contSpeechRecognizer.ContinuousRecognitionSession.StartAsync();


            this.ServerConnectButton.Click += async (s, a) =>
            {
                this.StartupSettings.Hide();
                await signaller.ConnectToServer(ServerConfig.AwsAddress);
                isConnectedToSource = false;

                if (signaller.connected)
                {
                    this.ConnectedOptions.Show();
                    this.CursorElement.Show();
                }
            };

            this.CursorElement.ManipulationDelta += async (s, e) =>
            {
                double newX = (this.t_Transform.TranslateX + e.Delta.Translation.X) / this.RemoteVideo.ActualWidth,
                    newY = (this.t_Transform.TranslateY + e.Delta.Translation.Y) / this.RemoteVideo.ActualHeight;

                //Logger.Log("newX: " + newX.ToString() + " newY: " + newY.ToString());
                if (newX < -threshold || newX > threshold
                    || newY < -threshold || newY > threshold)
                {
                    return;
                }
                
                if (this.isConnectedToSource)
                {
                    await this.cursorSignaller.SendMessage(
                        WebRtcSignaller.WebRtcMessage.CursorUpdate,
                        "{ x: " + newX.ToString() + ", y: " + newY.ToString() + "}"
                    );
                }
                this.t_Transform.TranslateX += e.Delta.Translation.X;
                this.t_Transform.TranslateY += e.Delta.Translation.Y;
            };

            var config = new ConductorConfig()
            {
                CoreDispatcher = this.Dispatcher,
                RemoteVideo = this.RemoteVideo,
                Signaller = signaller,
                Identity = "client"
            };

            await this.conductor.Initialize(config);

            var opts = new MediaOptions(
                new MediaOptions.Init()
                {
                    ReceiveVideo = (bool)this.ReceiveVideoCheck.IsChecked,
                    ReceiveAudio = (bool)this.ReceiveAudioCheck.IsChecked,
                    SendAudio = (bool)this.SendAudioCheck.IsChecked
                });
            this.conductor.SetMediaOptions(opts);

            this.conductor.UISignaller.ReceivedShutdown += async (s, a) =>
            {
                await this.conductor.Shutdown();
            };

            this.conductor.UISignaller.ReceivedPlain += (s, message) =>
            {
                Logger.Log(message);
            };

            this.conductor.UISignaller.ReceivedCursorUpdate += async (s, update) =>
            {
                await this.Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
                {
                    if (update.x < -threshold || update.x > threshold
                    || update.y < -threshold || update.y > threshold)
                    {
                        return;
                    }
                    this.t_Transform.TranslateX = update.x * this.RemoteVideo.ActualWidth;
                    this.t_Transform.TranslateY = update.y * this.RemoteVideo.ActualHeight;
                });
            };
        }


        #region UI_Handlers        

        private async void SayHiButton_Click(object sender, RoutedEventArgs e)
        {
            if (isProcessing) return;
            isProcessing = true;

            var message = "Hello, World!";
            await this.conductor.UISignaller.SendPlain(message);
            Logger.Log($"Sent {message} to any connected peers");

            isProcessing = false;
        }

        private async void SourceConnectButton_Click(object sender, RoutedEventArgs e)
        {            
            if (isProcessing) return;
            isProcessing = true;

            if (!isConnectedToSource)
            {
                Logger.Log("Starting connection to source...");
                var opts = new MediaOptions(
                new MediaOptions.Init()
                {
                    ReceiveVideo = (bool)this.ReceiveVideoCheck.IsChecked,
                    ReceiveAudio = (bool)this.ReceiveAudioCheck.IsChecked,
                    SendAudio = (bool)this.SendAudioCheck.IsChecked
                });
                this.conductor.SetMediaOptions(opts);
                await this.conductor.StartCall();
                Logger.Log("Connection started...");
                //signaller.DisconnectFromServer(); Because of async calls, this disconnects too early.
                this.SayHiButton.Hide();
            } else
            {
                Logger.Log("Disconnecting from source...");
                await this.conductor.Shutdown();
                Logger.Log("Connection ended...");
                this.SayHiButton.Show();
                this.ConnectedOptions.Hide();
                this.StartupSettings.Show();
            }
            isConnectedToSource = !isConnectedToSource;
            //this.SourceConnectButton.Content = (isConnectedToSource ? "Connect to Source" : "Disconnect from Source");
            // ^-- this doesn't work but the general logic is that the button should show "Disconnect" or "Connect" depending on its state

            // Temporary workaround: two buttons!
            if (isConnectedToSource)
            {
                this.SourceConnectButton.Hide();
                this.SourceDisconnectButton.Show();
            } else
            {
                this.SourceDisconnectButton.Hide();
                this.SourceConnectButton.Show();
            }

            isProcessing = false;
        }

        private async void ContinuousRecognitionSession_Completed(SpeechContinuousRecognitionSession sender, SpeechContinuousRecognitionCompletedEventArgs args)
        {
            /*await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                Logger.Log("Timeout.");
            });*/

            
            await contSpeechRecognizer.ContinuousRecognitionSession.StartAsync();
        }

        private async void ContSpeechRecognizer_HypothesisGenerated(
            SpeechRecognizer sender, SpeechRecognitionHypothesisGeneratedEventArgs args)
        {
            
            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                //Logger.Log(args.Hypothesis.Text);
                switch (args.Hypothesis.Text)
                {
                    case "full screen":
                        ApplicationView.GetForCurrentView().TryEnterFullScreenMode();
                        break;
                    case "exit full screen":
                        ApplicationView.GetForCurrentView().ExitFullScreenMode();
                        break;
                    case "zoom in":
                        ApplicationView.GetForCurrentView().TryResizeView(new Size(Width = this.ActualWidth * 1.5, Height = this.ActualHeight * 1.5));
                        break;
                    case "zoom out":
                        ApplicationView.GetForCurrentView().TryResizeView(new Size(Width = this.ActualWidth * 0.5, Height = this.ActualHeight * 0.5));
                        break;
                    case "minimize":
                        this.Hide();
                        break;
                    case "maximize":
                        this.Show();
                        break;
                }
            });
        }

        private async void ContinuousRecognitionSession_ResultGenerated(
        SpeechContinuousRecognitionSession sender, SpeechContinuousRecognitionResultGeneratedEventArgs args)
        {
            
            /*await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                Logger.Log("Waiting ...");
            });*/
        }
        #endregion
    }
}
