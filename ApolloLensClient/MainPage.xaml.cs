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
                await this.conductor.Shutdown();
            };
        }

        protected override async void OnNavigatedTo(NavigationEventArgs args)
        {
            #region ClientInitialization

            this.client = new SignallerClient("client");

            this.client.ConnectionSuccessUIHandler += async (s, a) =>
            {
                await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    this.client.RequestPollRooms();
                });
            };

            // Useful if signaller connection void before source <=> client connection established.
            // For example, if source disconnected prior to direct connection establishment.
            this.client.ConnectionEndedUIHandler += async (s, a) =>
            {
                await this.Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
                {
                    if (!this.conductor.CallInProgress())
                    {
                        this.ConnectedOptions.Hide();
                        this.StartupSettings.Show();
                        this.CursorElement.Hide();
                        this.SourceDisconnectButton.Hide();
                        this.SourceConnectButton.Show();
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
                    this.SourceDisconnectButton.Hide();
                    this.SourceConnectButton.Show();
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
                    if (update.x < -cursorThreshold || update.x > cursorThreshold
                        || update.y < -cursorThreshold || update.y > cursorThreshold)
                    {
                        return;
                    }

                    this.t_Transform.TranslateX = update.x * this.RemoteVideo.ActualWidth;
                    this.t_Transform.TranslateY = update.y * this.RemoteVideo.ActualHeight;
                });
            };

            this.client.MessageHandlers["RoomPoll"] += async (sender, message) =>
            {
                string[] rooms = message.Contents.Split(',');
                await this.Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
                {
                    this.JoinRoomComboBox.Items.Clear();
                    foreach (var room in rooms)
                    {
                        this.JoinRoomComboBox.Items.Add(room);
                    }
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

                // prevent sending out of bound data
                if (newX < -cursorThreshold || newX > cursorThreshold 
                    || newY < -cursorThreshold || newY > cursorThreshold)
                {
                    return;
                }

                if (this.conductor.CallInProgress())
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
            if (!this.client.IsInRoom()) return;

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
            if (!this.client.IsInRoom()) return;

            if (isProcessing) return;
            isProcessing = true;

            if (!this.conductor.CallInProgress())
            {
                Logger.Log("Starting connection to source...");
                var opts = new WebRtcConductor.MediaOptions(
                    new WebRtcConductor.MediaOptions.Init()
                    {
                        ReceiveVideo = (bool)this.ReceiveVideoCheck.IsChecked,
                        ReceiveAudio = (bool)this.ReceiveAudioCheck.IsChecked,
                        SendAudio = (bool)this.SendAudioCheck.IsChecked
                    }
                );
                this.conductor.SetMediaOptions(opts);
                await this.conductor.StartCall();
                Logger.Log("Connection started...");
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

            if (this.conductor.CallInProgress())
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
        private void JoinRoomButton_Click(object sender, RoutedEventArgs e)
        {
            this.client.RequestJoinRoom(this.JoinRoomComboBox.SelectedValue.ToString());
        }

        private void JoinRoomComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private async void CenterCursor()
        {
            Logger.Log("Centering cursor");
            CursorUpdate update_zoom = new CursorUpdate();
            update_zoom.x = update_zoom.y = 0;
            if (update_zoom.x < -cursorThreshold || update_zoom.x > cursorThreshold
                || update_zoom.y < -cursorThreshold || update_zoom.y > cursorThreshold)
            {
                return;
            }
            this.t_Transform.TranslateX = update_zoom.x * this.RemoteVideo.ActualWidth;
            this.t_Transform.TranslateY = update_zoom.y * this.RemoteVideo.ActualHeight;
            await this.client.SendMessage(
                "CursorUpdate",
                "{ x: " + update_zoom.x.ToString() + ", y: " + update_zoom.y.ToString() + "}"
            );
        }

        #endregion
    }
}
