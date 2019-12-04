using ApolloLensLibrary.Signaller;
using ApolloLensLibrary.Utilities;
using ApolloLensLibrary.WebRtc;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using Newtonsoft.Json;
using System;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace ApolloLensClient
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private SignallerClient client;
        private WebRtcConductor conductor;

        private Boolean isConnectedToSource = false;
        private bool isProcessing = false; /* we need a lock/mutex in this trick for users that click too many buttons */
        
        private const double cursorThreshold = 0.48;

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
                await this.conductor.SendShutdown();
                await this.conductor.Shutdown();
            };
        }

        protected override async void OnNavigatedTo(NavigationEventArgs args)
        {
            #region ClientInitialization

            this.client = new SignallerClient("client");

            // Useful if signaller connection void before source <=> client connection established.
            // For example, if source disconnected prior to direct connection establishment.
            this.client.ConnectionEndedUIHandler += async (s, a) =>
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

            this.client.ConnectionFailedUIHandler += async (s, a) =>
            {
                await this.Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
                {
                    this.ConnectedOptions.Hide();
                    this.StartupSettings.Show();
                    this.CursorElement.Hide();
                });
            };

            this.client.MessageHandlers["Plain"] += (sender, message) =>
            {
                Logger.Log(message.Contents);
            };

            this.client.MessageHandlers["CursorUpdate"] += async (sender, message) =>
            {
                var update = JsonConvert.DeserializeObject<CursorUpdate>(message.Contents);
                await this.Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
                {
                    this.t_Transform.TranslateX = update.x * this.RemoteVideo.ActualWidth;
                    this.t_Transform.TranslateY = update.y * this.RemoteVideo.ActualHeight;
                });
            };

            #endregion

            #region ConductorInitialization

            this.conductor = new WebRtcConductor();

            WebRtcConductor.Config config = new WebRtcConductor.Config()
            {
                CoreDispatcher = this.Dispatcher,
                RemoteVideo = this.RemoteVideo,
                Signaller = client
            };

            await this.conductor.Initialize(config);

            var opts = new WebRtcConductor.MediaOptions(
                new WebRtcConductor.MediaOptions.Init()
                {
                    ReceiveVideo = (bool)this.ReceiveVideoCheck.IsChecked,
                    ReceiveAudio = (bool)this.ReceiveAudioCheck.IsChecked,
                    SendAudio = (bool)this.SendAudioCheck.IsChecked
                });

            this.conductor.SetMediaOptions(opts);

            #endregion

            #region LambdaUIHandlers

            this.ServerConnectButton.Click += async (s, a) =>
            {
                this.StartupSettings.Hide();
                await this.client.ConnectToSignaller();
                isConnectedToSource = false;

                if (client.IsConnected)
                {
                    this.ConnectedOptions.Show();
                    this.CursorElement.Show();
                }
            }; 
            
            this.CursorElement.ManipulationDelta += async (s, e) =>
            {
                double newX = (this.t_Transform.TranslateX + e.Delta.Translation.X) / this.RemoteVideo.ActualWidth,
                    newY = (this.t_Transform.TranslateY + e.Delta.Translation.Y) / this.RemoteVideo.ActualHeight;

                if (this.isConnectedToSource)
                {
                    await this.client.SendMessage(
                        "CursorUpdate",
                        "{ x: " + newX.ToString() + ", y: " + newY.ToString() + "}"
                    );
                }
                this.t_Transform.TranslateX += e.Delta.Translation.X;
                this.t_Transform.TranslateY += e.Delta.Translation.Y;
            };

            #endregion

        }


        #region FunctionalUIHandlers        

        private async void SayHiButton_Click(object sender, RoutedEventArgs e)
        {
            if (isProcessing) return;
            isProcessing = true;

            var message = "Hello, World!";
            //await this.conductor.UISignaller.SendPlain(message);
            await this.client.SendMessage("Plain", message);
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
                var opts = new WebRtcConductor.MediaOptions(
                new WebRtcConductor.MediaOptions.Init()
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

        #endregion
    }
}
