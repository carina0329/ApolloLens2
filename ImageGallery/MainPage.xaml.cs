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
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using System.ComponentModel;
using ApolloLensLibrary.Imaging;
using ApolloLensLibrary.Utilities;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using Windows.Networking;
using Windows.Storage.Streams;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.UI.ViewManagement;
using Windows.Media.SpeechRecognition;
using Windows.Media.Capture;
using Windows.ApplicationModel.Resources.Core;
using Windows.UI.Popups;
using Windows.System.Threading;
using Windows.UI.Core;



// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace ScanGallery
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        private IImageCollection ImageCollection { get; set; }
        private SoftwareBitmap sbimg { get; set; }

        #region Bindings

        public Array Stretches { get; } = Enum.GetNames(typeof(Stretch));

        public IEnumerable<string> SeriesNamesItems => 
            this.ImageCollection?.GetSeriesNames().Select((s, i) => $"{i + 1}. {s}");

        public SoftwareBitmapSource SoftwareBitmapSource { get; set; }

        private string ServerAddressKey { get; } = "CustomServerAddress";
        public string ServerAddress { get; set; }

        private int index;
        public int Index
        {
            get
            {
                return this.index;
            }
            set
            {
                var moveImage = this.index < value ?
                    new Action(() => this.ImageCollection.MoveNext()) :
                    new Action(() => this.ImageCollection.MovePrevious());

                foreach (var i in Util.Range(Math.Abs(this.index - value)))
                {
                    moveImage();
                }
            }
        }

        #endregion

        #region Startup

        public MainPage()
        {
            this.DataContext = this;
            this.InitializeComponent();
            this.SoftwareBitmapSource = new SoftwareBitmapSource();

            if (!ApplicationData.Current.LocalSettings.Values.TryGetValue(this.ServerAddressKey, out object value))
            {
                value = "10.0.0.192";
                ApplicationData.Current.LocalSettings.Values["CustomServerAddress"] = (string)value;
            }
            this.ServerAddress = (string)value;
            this.OnPropertyChanged(nameof(this.ServerAddress));
        }

        #endregion

        #region INotify

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region LoadingUIHandlers

        private async void LoadStudy_Click(object sender, RoutedEventArgs e)
        {
            this.LoadSettings.Hide();

            var loader = new DicomNetworking();
            loader.ReadyToLoad += (s, num) =>
            {
                this.LoadBarPanel.Show();

                this.LoadingBar.Maximum = num;
            };

            loader.LoadedImage += (s, a) =>
            {
                this.LoadingBar.Value++;
            };

            var client = new StreamSocket();
            await client.ConnectAsync(new HostName(this.ServerAddress), "8080");

            this.ImageCollection = await loader.LoadStudyAsync(client.InputStream);
            await this.OnStudyLoaded();
        }

        private async void LoadStudyLocal_Click(object sender, RoutedEventArgs e)
        {
            this.LoadSettings.Hide();

            var parser = new ImageLibrary();
            parser.ReadyToLoad += async (s, num) =>
            {
                await this.RunOnUi(() =>
                {
                    this.LoadBarPanel.Show();

                    this.LoadingBar.Maximum = num;
                });
            };

            parser.LoadedImage += async (s, a) =>
            {
                await this.RunOnUi(() =>
                {
                    this.LoadingBar.Value++;
                });
            };

            this.sbimg = await parser.GetStudyAsCollection();
            await this.OnStudyLoaded();
        }

        private async Task OnStudyLoaded()
        {
                await this.RunOnUi(async () =>
                {
                    this.SoftwareBitmapSource = new SoftwareBitmapSource();
                    await this.SoftwareBitmapSource.SetBitmapAsync(sbimg);
                    this.OnPropertyChanged(nameof(this.SoftwareBitmapSource));
                });

            this.LoadingScreen.Hide();
            this.RunningScreen.Show();

            this.OnPropertyChanged(nameof(this.SeriesNamesItems));
            this.StretchSelect.SelectedIndex = 2;

        }

        #endregion

        #region UIHandlers

        private void StretchSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var str = (string)this.StretchSelect.SelectedItem;
            Enum.TryParse(typeof(Stretch), str, out object stretch);
            this.Image.Stretch = (Stretch)stretch;
        }

        private void ServerAddressBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            var textBox = (TextBox)sender;
            ApplicationData.Current.LocalSettings.Values[this.ServerAddressKey] = textBox.Text;
        }

        #endregion

        #region Speech
        private ThreadPoolTimer Scroll { get; set; }
        #endregion

        #region Utility
        private async Task RunOnUi(Action action)
        {
            await this.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal, () => action());
        }
        #endregion
    }
}
