using ApolloLensLibrary.Signalling;
using ApolloLensLibrary.Utilities;
using ApolloLensLibrary.WebRtc;
using System;
using System.Linq;
using WebRtcImplNew;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace ApolloLensAudio
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private IConductor conductor { get; } = Conductor.Instance;

        private WebsocketSignaller signaller = null;

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
            signaller = new WebsocketSignaller();

            this.SetUpCallButton.Click += async (s, a) =>
            {
                this.NotConnected.Hide();
                await signaller.ConnectToServer("ws://drhololens-env.esncizasfm.us-east-2.elasticbeanstalk.com/");
                this.Connected.Show();
            };

            this.HangUpCallButton.Click += (s, a) =>
            {
                this.Connected.Hide();
                this.conductor.Shutdown();
                this.NotConnected.Show();
            };

            this.DisconnectFromServerButton.Click += (s, a) =>
            {
                Logger.Log("Disconnected from server");
                signaller.DisconnectFromServer();
            };

            var config = new ConductorConfig()
            {
                CoreDispatcher = this.Dispatcher,
                RemoteVideo = this.RemoteVideo,
                Signaller = signaller
            };

            Logger.Log("Initializing WebRTC...");
            await this.conductor.Initialize(config);
            Logger.Log("Done.");

            var opts = new MediaOptions(
                new MediaOptions.Init()
                {
                    SendAudio = true,
                    ReceiveAudio = true
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

        }

        #region UI_Handlers

        private async void SayHiButton_Click(object sender, RoutedEventArgs e)
        {
            var message = "Hi, I'm ready to call!";
            await this.conductor.UISignaller.SendPlain(message);
            Logger.Log($"Send message: {message} to connected peer");
        }

        #endregion


        private async void StartCallButton_Click(object sender, RoutedEventArgs e)
        {
            Logger.Log("Starting connection to source...");
            await this.conductor.StartCall();
            Logger.Log("Connection started...");
            signaller.DisconnectFromServer();
        }
    }
}
