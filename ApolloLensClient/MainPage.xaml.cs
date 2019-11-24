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
            var signaller = new WebsocketSignaller();

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
                this.ConnectedOptions.Show();
            };

            var config = new ConductorConfig()
            {
                CoreDispatcher = this.Dispatcher,
                RemoteVideo = this.RemoteVideo,
                Signaller = signaller
            };

            await this.conductor.Initialize(config);

            var opts = new MediaOptions(
                new MediaOptions.Init()
                {
                    ReceiveVideo = false
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
            var message = "Hello, World!";
            await this.conductor.UISignaller.SendPlain(message);
            Logger.Log($"Sent {message} to any connected peers");
        }

        private async void SourceConnectButton_Click(object sender, RoutedEventArgs e)
        {
            Logger.Log("Starting connection to source...");
            await this.conductor.StartCall();
            Logger.Log("Connection started...");
        }

        private async void ContinuousRecognitionSession_Completed(SpeechContinuousRecognitionSession sender, SpeechContinuousRecognitionCompletedEventArgs args)
        {
            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                Logger.Log("Timeout.");
            });

            
            await contSpeechRecognizer.ContinuousRecognitionSession.StartAsync();
        }

        private async void ContSpeechRecognizer_HypothesisGenerated(
            SpeechRecognizer sender, SpeechRecognitionHypothesisGeneratedEventArgs args)
        {
            
            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                Logger.Log(args.Hypothesis.Text);
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
            
            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                Logger.Log("Waiting ...");
            });
        }
        #endregion
    }
}
