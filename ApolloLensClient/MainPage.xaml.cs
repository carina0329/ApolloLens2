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
        public SpeechRecognizer speechRecognizer = new SpeechRecognizer();
        private CoreDispatcher dispatcher;

        public MainPage()
        {
            this.DataContext = this;
            this.InitializeComponent();
            string[] responses = { "Zoom In", "Zoom Out", "Maximize", "Exit Full Screen", "Full Screen", "Minimize" };
            var listConstraint = new SpeechRecognitionListConstraint(responses, "Resize");
            speechRecognizer.Constraints.Add(listConstraint);
            speechRecognizer.ContinuousRecognitionSession.ResultGenerated += ContinuousRecognitionSession_ResultGenerated;

            Logger.WriteMessage += async (message) =>
            {
                //var result = await SpeechRec();
                Console.WriteLine(await SpeechRec());
            };
            
            var result = SpeechRec();
            //Console.WriteLine(result);
            

            Logger.WriteMessage += async (message) =>
            {
                await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    this.OutputTextBox.Text += message + Environment.NewLine;
                });
            };

            //Logger.Log(result);

            Application.Current.Suspending += async (s, e) =>
            {
                await this.conductor.UISignaller.SendShutdown();
                await this.conductor.Shutdown();
            };

        }

        protected override async void OnNavigatedTo(NavigationEventArgs args)
        {
            var signaller = new WebsocketSignaller();

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
                    ReceiveVideo = true
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

        public async Task<string> SpeechRec() {
            await speechRecognizer.CompileConstraintsAsync();
            SpeechRecognitionResult speechRecognitionResult = await speechRecognizer.RecognizeAsync();
            Logger.Log(speechRecognitionResult.Text);
            switch (speechRecognitionResult.Text)
            {
                case "Full Screen":
                    ApplicationView.GetForCurrentView().TryEnterFullScreenMode();
                    break;
                case "Exit Full Screen":
                    ApplicationView.GetForCurrentView().ExitFullScreenMode();
                    break;
                case "Zoom In":
                    ApplicationView.GetForCurrentView().TryResizeView(new Size(Width = this.ActualWidth * 1.5, Height = this.ActualHeight * 1.5));
                    break;
                case "Zoom Out":
                    ApplicationView.GetForCurrentView().TryResizeView(new Size(Width = this.ActualWidth * 0.5, Height = this.ActualHeight * 0.5));
                    break;
                case "Minimize":
                    this.Hide();
                    break;
                case "Maximize":
                    this.Show();
                    break;
            }
            return speechRecognitionResult.Text;
        }

        private void ContinuousRecognitionSession_ResultGenerated(
          SpeechContinuousRecognitionSession sender,
          SpeechContinuousRecognitionResultGeneratedEventArgs args)
            {
                
                if (args.Result.Confidence == SpeechRecognitionConfidence.Medium ||
                  args.Result.Confidence == SpeechRecognitionConfidence.High)
                {
                    Logger.Log(args.Result.Text + " ");

                    //await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    //{
                    //    dictationTextBox.Text = dictatedTextBuilder.ToString();
                    //    btnClearText.IsEnabled = true;
                    //});
                }
                //else
                //{
                //    await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                //    {
                //        dictationTextBox.Text = dictatedTextBuilder.ToString();
                //    });
                //}
        }
        #endregion
    }
}
