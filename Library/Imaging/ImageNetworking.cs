using ApolloLensLibrary.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;


namespace ApolloLensLibrary.Imaging
{
    /// <summary>
    /// Defines a two sided protocol for sending and 
    /// receiving a Dicom study over input and output
    /// streams. 
    /// </summary>
    public class ImageNetworking
    {
        #region Events

        public event EventHandler<int> ReadyToLoad;
        public event EventHandler LoadedImage;
        public event EventHandler SentImage;

        private void OnReceivedNumImages(int numImages)
        {
            this.ReadyToLoad?.Invoke(this, numImages);
        }

        private void OnLoadedImage()
        {
            this.LoadedImage?.Invoke(this, EventArgs.Empty);
        }

        private void OnSentImage()
        {
            this.SentImage?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region Interface

        public async Task SendStudyAsync(SoftwareBitmap images, IOutputStream stream)
        {
            using (var writer = new DataWriter(stream))
            {
                Windows.Storage.Streams.Buffer buffer = new Windows.Storage.Streams.Buffer(1000);
                images.CopyToBuffer(buffer);
                writer.WriteBuffer(buffer);
                        await writer.StoreAsync();
                        this.OnSentImage();
            }
        }

        public async Task<SoftwareBitmap> LoadStudyAsync(IInputStream stream)
        {
            FileRandomAccessStream fstream = (FileRandomAccessStream)stream;
            BitmapDecoder decoder = await BitmapDecoder.CreateAsync(fstream);
            SoftwareBitmap softbitmap = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
            return softbitmap;
        }

        #endregion

        private async Task LoadSeriesAsync(DataReader reader,SoftwareBitmap sbimg)
        {
            await reader.LoadAsync(sizeof(int));
            var nameLength = reader.ReadUInt32();

            await reader.LoadAsync(nameLength);
            var seriesName = reader.ReadString(nameLength);

            await reader.LoadAsync(sizeof(int));
            var numImages = reader.ReadInt32();

            //imageCollection.CreateSeries(seriesName, numImages);
            foreach (var position in Util.Range(numImages))
            {
                await this.LoadImageAsync(
                    reader, seriesName, position, sbimg);
            }
        }

        private async Task LoadImageAsync(DataReader reader, string seriesName, int position, SoftwareBitmap sbimg)
        {
            await reader.LoadAsync(sizeof(int) * 2 + sizeof(uint));
            var width = reader.ReadInt32();
            var height = reader.ReadInt32();
            var bufferLength = reader.ReadUInt32();

            await reader.LoadAsync(bufferLength);

            var imageBytes = new byte[bufferLength];
            reader.ReadBytes(imageBytes);

            this.OnLoadedImage();
        }
    }
}
