using ApolloLensLibrary.Signaller;
using ApolloLensLibrary.Utilities;
using ApolloLensLibrary.WebRtc;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using System;
using System.Linq;


// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace ApolloLensSource
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private SignallerClient client;
        private WebRtcConductor conductor;

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

            this.client = new SignallerClient("source");

            this.client.ConnectionFailedUIHandler += async (s, a) =>
            {
                await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    this.Connected.Hide();
                    this.NotConnected.Show();
                });
            };

            this.client.RoomCreateFailedUIHandler += async (s, a) =>
            {
                await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    this.StatusText.Text = "Status: Room Creation Failed (either empty room or already exists)";
                });
            };

            this.client.RoomJoinSuccessUIHandler += async (s, a) =>
            {
                await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    this.StatusText.Text = $"Status: Joined Room {this.client.RoomId}";
                });
            };

            this.client.RoomJoinFailedUIHandler += async (s, a) =>
            {
                await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    this.StatusText.Text = "Status: Room Join Failed (Nonexistent room or because source already exists)";
                });
            };

            this.client.MessageHandlers["Plain"] += (sender, message) =>
            {
                Logger.Log(message.Contents);
            };

            #endregion

            #region ConductorInitialization

            this.conductor = new WebRtcConductor();

            var config = new WebRtcConductor.Config()
            {
                CoreDispatcher = this.Dispatcher,
                Signaller = client,
            };

            Logger.Log("Initializing WebRTC...");
            await this.conductor.Initialize(config);
            Logger.Log("Done.");

            var opts = new WebRtcConductor.MediaOptions(
                new WebRtcConductor.MediaOptions.Init()
                {
                    SendVideo = true
                });

            this.conductor.SetMediaOptions(opts);

            var devices = await this.conductor.GetVideoDevices();
            this.MediaDeviceComboBox.ItemsSource = devices;
            this.MediaDeviceComboBox.SelectedIndex = 0;

            this.CaptureFormatComboBox.ItemsSource =
                await this.conductor.GetCaptureProfiles(devices.First());
            this.CaptureFormatComboBox.SelectedIndex = 0;

            #endregion

            #region LambdaUIHandlers

            this.ConnectToServerButton.Click += async (s, a) =>
            {
                this.NotConnected.Hide();
                await client.ConnectToSignaller();
                if (client.IsConnected) this.Connected.Show();
                this.StatusText.Text = "Status: Connected to Signaller";
            };

            this.DisconnectFromServerButton.Click += async (s, a) =>
            {
                this.Connected.Hide();
                client.DisconnectFromSignaller();
                this.NotConnected.Show();
                this.StatusText.Text = "Status: Disconnected From Signaller";
            };

            #endregion
        }

        #region FunctionalUIHandlers

        private async void SayHiButton_Click(object sender, RoutedEventArgs e)
        {
            if (!this.client.IsInRoom()) return;
            var message = "Hello, World!";
            //await this.conductor.UISignaller.SendPlain(message);
            await this.client.SendMessage("Plain", message);
            Logger.Log($"Send message: {message} to connected peers");
        }

        private void CaptureFormatComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedProfile = (this.CaptureFormatComboBox.SelectedItem as WebRtcConductor.CaptureProfile);
            this.conductor.SetSelectedProfile(selectedProfile);
        }

        private async void MediaDeviceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var mediaDevice = (this.MediaDeviceComboBox.SelectedItem as WebRtcConductor.VideoDevice);
            this.conductor.SetSelectedMediaDevice(mediaDevice);

            this.CaptureFormatComboBox.ItemsSource =
                await this.conductor.GetCaptureProfiles(mediaDevice);
            this.CaptureFormatComboBox.SelectedIndex = 0;
        }

        private void CreateRoomButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.CreateRoomTextBox.Text == "Room Name...")
            {
                this.StatusText.Text = "Status: Invalid Room Name";
            }
            else
            {
                this.client.RequestCreateRoom(this.CreateRoomTextBox.Text);
            }
        }

        #endregion
    }
}
