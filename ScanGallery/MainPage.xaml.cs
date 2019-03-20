﻿using System;
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
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using Windows.Networking;
using Windows.Storage.Streams;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace ScanGallery
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        private DicomManager dicom { get; set; }
        private IDicomStudy ImageCollection { get; set; }

        // Bound to MainPage.xaml
        public WriteableBitmap Image => this.ImageCollection.GetCurrentImage();
        public IList<string> Series => this.ImageCollection.GetSeriesNames();

        public MainPage()
        {
            this.DataContext = this;
            this.InitializeComponent();
            this.dicom = new DicomManager();
            this.ImageCollection = new ImageCollection();

            // Pass property changes in ImageCollection up to MainPage.xaml
            this.ImageCollection.PropertyChanged += (s, e) =>
            {
                // Translate ImageCollection method name to local property name
                var bindings = new Dictionary<string, string>()
                {
                    { "GetCurrentImage", "Image" },
                    { "GetSeriesNames", "Series" }
                };
                var name = bindings[e.PropertyName];
                this.OnPropertyChanged(name);
            };
        }

        private void OnPropertyChanged(string propertyName)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            var load = Task.Run(() => this.LoadAsync());
        }

        private async Task LoadAsync()
        {
            var client = new StreamSocket();
            await client.ConnectAsync(new HostName("10.0.0.192"), "9000");

            using (var reader = new DataReader(client.InputStream))
            {
                var numSeries = reader.ReadInt32();
                foreach (var series in Range(numSeries))
                {
                    var nameLength = reader.ReadUInt32();
                    var seriesName = reader.ReadString(nameLength);
                    var numImages = reader.ReadInt32();
                    foreach (var position in Range(numImages))
                    {
                        var width = reader.ReadInt32();
                        var height = reader.ReadInt32();
                        var bufferLength = reader.ReadUInt32();
                        var buffer = reader.ReadBuffer(bufferLength);

                        var bitmap = new WriteableBitmap(width, height);
                        using (var source = buffer.AsStream())
                        {
                            using (var destination = bitmap.PixelBuffer.AsStream())
                            {
                                await source.CopyToAsync(destination);
                            }
                        }

                        this.ImageCollection.InsertImageInSeries(bitmap, seriesName, position);
                    }
                }
            }
        }

        private IEnumerable<int> Range(int count)
        {
            return Enumerable.Range(0, count);
        }

        private void BrightnessUp_Click(object sender, RoutedEventArgs e)
        {
            this.ImageCollection.IncreaseBrightness();
        }

        private void ContrastUp_Click(object sender, RoutedEventArgs e)
        {
            this.ImageCollection.IncreaseContrast();
        }

        private void ContrastDown_Click(object sender, RoutedEventArgs e)
        {
            this.ImageCollection.DecreaseContrast();
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            this.ImageCollection.Reset();
        }

        private void BrightnessDown_Click(object sender, RoutedEventArgs e)
        {
            this.ImageCollection.DecreaseBrightness();
        }

        private void Previous_Click(object sender, RoutedEventArgs e)
        {
            this.ImageCollection.MovePrevious();
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            this.ImageCollection.MoveNext();
        }

        private void SeriesSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var seriesName = (string)this.SeriesSelect.SelectedItem;
            this.ImageCollection.SetCurrentSeries(seriesName);
        }
    }
}
