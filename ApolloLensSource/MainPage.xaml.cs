﻿using ApolloLensLibrary.Signalling;
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

namespace ApolloLensSource
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private IConductor conductor { get; } = Conductor.Instance;
        private SignallerClient client { get; set; }

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
            client = new SignallerClient("source");

            client.ConnectionFailedUIHandler += (s, a) =>
            {
                this.Connected.Hide();
                this.NotConnected.Show();
            };

            

            //var signaller = new WebsocketSignaller("source");

            //signaller.ConnectionFailed += (s, a) =>
            //{
            //    this.Connected.Hide();
            //    this.NotConnected.Show();
            //};

            this.ConnectToServerButton.Click += async (s, a) =>
            {
                this.NotConnected.Hide();        
                await client.ConnectToSignaller();
                if (client.IsConnected) this.Connected.Show();
            };

            this.DisconnectFromServerButton.Click += (s, a) =>
            {
                this.Connected.Hide();
                client.DisconnectFromSignaller();
                this.NotConnected.Show();
            };

            client.ReceivedMessageExternalHandler += (sender, message) =>
            {
                switch (message.Type)
                {
                    case "Plain":
                        Logger.Log(message.Contents);
                        break;
                }
            };

            //var config = new ConductorConfig()
            //{
            //    CoreDispatcher = this.Dispatcher,
            //    Signaller = signaller,
            //    Identity = "source"
            //};

            //Logger.Log("Initializing WebRTC...");
            //await this.conductor.Initialize(config);
            //Logger.Log("Done.");

            //var opts = new MediaOptions(
            //    new MediaOptions.Init()
            //    {
            //        SendVideo = true
            //    });
            //this.conductor.SetMediaOptions(opts);

            //this.conductor.UISignaller.ReceivedShutdown += async (s, a) =>
            //{
            //    await this.conductor.Shutdown();
            //};

            //this.conductor.UISignaller.ReceivedPlain += (s, message) =>
            //{
            //    Logger.Log(message);
            //};

            //var devices = await this.conductor.GetVideoDevices();
            //this.MediaDeviceComboBox.ItemsSource = devices;
            //this.MediaDeviceComboBox.SelectedIndex = 0;

            //this.CaptureFormatComboBox.ItemsSource =
            //    await this.conductor.GetCaptureProfiles(devices.First());
            //this.CaptureFormatComboBox.SelectedIndex = 0;
        }

        #region UI_Handlers

        private async void SayHiButton_Click(object sender, RoutedEventArgs e)
        {
            var message = "Hello, World!";
            //await this.conductor.UISignaller.SendPlain(message);
            await this.client.SendMessage(this.client.MessageType["Plain"], message);
            Logger.Log($"Send message: {message} to connected peers");
        }

        private void CaptureFormatComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedProfile = (this.CaptureFormatComboBox.SelectedItem as CaptureProfile);
            this.conductor.SetSelectedProfile(selectedProfile);
        }

        #endregion

        private async void MediaDeviceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var mediaDevice = (this.MediaDeviceComboBox.SelectedItem as VideoDevice);
            this.conductor.SetSelectedMediaDevice(mediaDevice);

            this.CaptureFormatComboBox.ItemsSource =
                await this.conductor.GetCaptureProfiles(mediaDevice);
            this.CaptureFormatComboBox.SelectedIndex = 0;
        }
    }
}
