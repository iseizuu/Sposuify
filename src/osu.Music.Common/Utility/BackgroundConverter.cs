using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using Osu.Music.Common.Models;
using Osu.Music.Common.Utility;

namespace Osu.Music.Common.Utility
{
    public class BackgroundConverter : IValueConverter
    {
        private static readonly Dictionary<string, string> _cache = new Dictionary<string, string>();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Beatmap map)
            {
                if (string.IsNullOrEmpty(map.MD5Hash))
                {
                    return BeatmapFileReader.GetBackgroundPath(map);
                }

                if (_cache.ContainsKey(map.MD5Hash))
                    return _cache[map.MD5Hash];
                string path = BeatmapFileReader.GetBackgroundPath(map);

                if (!string.IsNullOrEmpty(path))
                    _cache[map.MD5Hash] = path;

                return path;
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}