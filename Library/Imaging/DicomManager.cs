﻿using Dicom;
using Dicom.Imaging;
using Dicom.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml.Media.Imaging;

namespace ApolloLensLibrary.Imaging
{
    public class DicomManager
    {
        public static async Task<IDictionary<string, IEnumerable<WriteableBitmap>>> GetStudy()
        {
            var directory = await GetDicomDirectory();
            var imagePaths = await directory.GetImagePaths();

            var collection = new List<Tuple<string, int, WriteableBitmap>>(imagePaths.Count());
            foreach (var path in imagePaths)
            {
                collection.Add(await ProcessImagePath(directory, path));
            }

            return collection
                .GroupBy(t => t.Item1)
                .ToDictionary(
                    grp => grp.Key,
                    grp => grp.OrderBy(t => t.Item2).Select(t => t.Item3));
        }

        private static async Task<StorageFolder> GetDicomDirectory()
        {
            var folderPicker = new FolderPicker();
            folderPicker.SuggestedStartLocation = PickerLocationId.Desktop;
            folderPicker.FileTypeFilter.Add("*");

            return await folderPicker.PickSingleFolderAsync();
        }

        private static async Task<Tuple<string, int, WriteableBitmap>> ProcessImagePath(StorageFolder directory, string path)
        {
            var file = await directory.GetFileAsync(path);
            var stream = await file.OpenStreamForReadAsync();
            var dicomFile = await DicomFile.OpenAsync(stream);

            var xdoc = XDocument.Parse(dicomFile.Dataset.WriteToXml());

            var position = xdoc
                .GetElementsByDicomKeyword("InstanceNumber")
                .Select(elt => Convert.ToInt32(elt.Value))
                .FirstOrDefault();

            var series = xdoc
                .GetElementsByDicomKeyword("SeriesNumber")
                .Select(elt => elt.Value)
                .FirstOrDefault();

            var dicomImage = new DicomImage(dicomFile.Dataset);
            var bitmap = dicomImage.RenderImage().AsWriteableBitmap();

            return Tuple.Create(series, position, bitmap);
        }
    }

    public static class DicomExtensions
    {
        public static IEnumerable<XElement> GetElementsByDicomKeyword(this XDocument xdoc, string keyword)
        {
            return xdoc
                .Descendants("DicomAttribute")
                .Where(elt =>
                {
                    return elt
                        .Attribute("keyword")
                        .Value
                        .Contains(keyword);
                });
        }

        public static async Task<string> GetDicomDirFileName(this StorageFolder directory)
        {
            var files = await directory.GetFilesAsync();

            return files
                .Select(file => file.Name)
                .Where(name => name.ToLower().Contains("dicomdir"))
                .FirstOrDefault();
        }

        public static async Task<IEnumerable<string>> GetImagePaths(this StorageFolder directory)
        {
            var fileName = await directory.GetDicomDirFileName();
            if (fileName != null)
            {
                return await directory.GetPathsFromDicomDirFile(fileName);
            }
            return await directory.GetPathsFromDirectory();
        }

        public static async Task<IEnumerable<string>> GetPathsFromDicomDirFile(this StorageFolder directory, string fileName)
        {
            var dicomdirFile = await directory.GetFileAsync(fileName);
            var stream = await dicomdirFile.OpenStreamForReadAsync();
            var dicomFile = await DicomFile.OpenAsync(stream);

            var xdoc = XDocument.Parse(dicomFile.Dataset.WriteToXml());

            return xdoc
                .GetElementsByDicomKeyword("ReferencedFileID")
                .Select(elt =>
                {
                    var vals = elt
                        .Descendants()
                        .Select(val => val.Value);

                    return string.Join(@"\", vals);
                })
                .ToList();
        }

        public static async Task<IEnumerable<string>> GetPathsFromDirectory(this StorageFolder directory)
        {
            var files = await directory.GetFilesAsync();
            return files
                .Select(file => file.Name);
        }
    }


}
