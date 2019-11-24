﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;

namespace ApolloLensLibrary.Imaging
{
    /// <summary>
    /// Responsible for loading a Dicom study from
    /// disk and returing it as an IImageCollection
    /// or raw dictionary of ImageTransferObjects.
    /// </summary>
    /// <remarks>
    /// FoDicom was needed primarily to parse dicom
    /// files to XML for data extraction, and to 
    /// render dicom image files to bitmaps.
    /// dicomFile.Dataset.WriteToXml() was used because
    /// the developer had more experience using Linq
    /// to XML than the FoDicom API.
    /// </remarks>
    public class ImageLibrary
    {
        public SoftwareBitmap softbitmap { get; set; }
        #region Events

        public event EventHandler<int> ReadyToLoad;
        public event EventHandler LoadedImage;

        private void OnReadyToLoad(int numImages)
        {
            this.ReadyToLoad?.Invoke(this, numImages);
        }

        private void OnLoadedImage()
        {
            this.LoadedImage?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region Interface

        /// <summary>
        /// Wraps this.GetStudyRaw(), and builds an ImageCollection
        /// from the resulting collection.
        /// </summary>
        /// <returns></returns>
        public async Task<SoftwareBitmap> GetStudyAsCollection()
        {
            var raw = await this.GetStudyRaw();
            return raw;
            //var result = ImageCollection.Create();

            //foreach (var series in raw.Keys)
            //{
            //    result.CreateSeries(series, raw[series].Count());
            //    foreach (var im in raw[series])
            //    {
            //        result.AddImageToSeries(im);
            //    }
            //}

            //return result;
        }

        /// <summary>
        /// Prompts user for a directory, then loads the
        /// Dicom study contained in that directory into
        /// memory.
        /// </summary>
        /// <returns></returns>
        public async Task<SoftwareBitmap> GetStudyRaw()
        {
            // prompt user for directory
            var file = await this.GetImageFile();
            FileRandomAccessStream fstream = (FileRandomAccessStream)await file.OpenAsync(FileAccessMode.Read);
            BitmapDecoder decoder = await BitmapDecoder.CreateAsync(fstream);
            softbitmap = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
            return softbitmap;
            //var inputstream = await file.OpenSequentialReadAsync();
            //    var readStream = inputstream.AsStreamForRead();
            //    var byteArray = new byte[readStream.Length];
            //    await readStream.ReadAsync(byteArray, 0, byteArray.Length);
            //// TODO: add error checking?
            //BitmapImage bimg = new BitmapImage();
            //FileRandomAccessStream stream = (FileRandomAccessStream)await file.OpenAsync(FileAccessMode.Read);
            //bimg.SetSource(stream);
            //////MemoryStream memstream = new MemoryStream();
            //////bimg.Save(memstream, bimg.RawFormat);
            //////byte[] byteArray = memstream.ToArray();
            ////byte[] byteArray = new byte[stream.Size];
            ////await stream.ReadAsync(byteArray, 0, byteArray.Length);
            //var collection = new List<ImageTransferObject>();
            //var image = new ImageTransferObject()
            //{
            //    Image = byteArray,
            //    Width = bimg.PixelWidth,
            //    Height = bimg.PixelHeight,
            //    Position = 0,
            //    Series = "series"
            //};
            //collection.Add(image);
            this.OnLoadedImage();

            // clean up collection, transform List<ImageTransferObject> into
            // IDictionary<string, IEnumerable<ImageTransferObject>>.
            // Also clean up the position field of each ImageTransferObject
            // to reflect the image's actual location in the list.
            //var res = collection
            //    .GroupBy(transfer => transfer.Series)
            //    .ToDictionary(
            //        grp => grp.Key,
            //        grp =>
            //        {
            //            var ordered = grp
            //                .OrderBy(transfer => transfer.Position);

            //            foreach (var it in ordered
            //                .Select((t, i) => new { value = t, idx = i }))
            //            {
            //                it.value.Position = it.idx;
            //            }

            //            return ordered.AsEnumerable();
            //        });

            //return res;
        }

        #endregion

        /// <summary>
        /// Prompt user for a directory and return the resulting
        /// storage folder async
        /// </summary>
        /// <returns></returns>
        private async Task<StorageFile> GetImageFile()
        {
            var fileOpenPicker = new FileOpenPicker();
            fileOpenPicker.SuggestedStartLocation = PickerLocationId.Desktop;
            fileOpenPicker.FileTypeFilter.Add(".jpg");
            fileOpenPicker.FileTypeFilter.Add(".jpeg");
            fileOpenPicker.FileTypeFilter.Add(".png");
            fileOpenPicker.FileTypeFilter.Add(".bmp");
            fileOpenPicker.FileTypeFilter.Add(".gif");
            fileOpenPicker.FileTypeFilter.Add(".tiff");

            return await fileOpenPicker.PickSingleFileAsync();
        }
    }
}