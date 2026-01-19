using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using Osu.Music.Common.Models;

namespace Osu.Music.Common.Utility
{
    public enum CoverMode
    {
        Thumbnail,
        Full
    }

    public class BeatmapCoverConverter : IValueConverter
    {
        private static readonly Dictionary<string, BitmapImage> _thumbCache = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not Beatmap map)
                return null;

            var path = BeatmapFileReader.GetBackgroundPath(map);
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return null;

            var mode = CoverMode.Thumbnail;

            if (parameter is string p && Enum.TryParse(p, true, out CoverMode parsed))
                mode = parsed;
            if (mode == CoverMode.Thumbnail)
            {
                if (_thumbCache.TryGetValue(path, out var cached))
                    return cached;

                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri(path);
                bmp.DecodePixelWidth = 96; 
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                bmp.Freeze();

                _thumbCache[path] = bmp;
                return bmp;
            }

            var full = new BitmapImage();
            full.BeginInit();
            full.UriSource = new Uri(path);
            full.CacheOption = BitmapCacheOption.OnLoad;
            full.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            full.EndInit();
            full.Freeze();

            return full;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
