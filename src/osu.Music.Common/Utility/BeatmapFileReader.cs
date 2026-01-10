using Osu.Music.Common.Models;
using osu_database_reader;
using osu_database_reader.Components.Events;
using osu_database_reader.TextFiles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Osu.Music.Common.Utility
{
    public static class BeatmapFileReader
    {
        public static BeatmapFile Read(Stream stream)
        {
            var file = new BeatmapFile();

            using var r = new StreamReader(stream);
            string firstLine = r.ReadLine();
            if (string.IsNullOrEmpty(firstLine) || !firstLine.StartsWith("osu file format v"))
                throw new Exception("Not a valid beatmap");

            if (!int.TryParse(firstLine.Replace("osu file format v", string.Empty), out file.FileFormatVersion))
                file.FileFormatVersion = 14;

            BeatmapSection? bs;
            while ((bs = r.ReadUntilSectionStart()) != null)
            {
                switch (bs.Value)
                {
                    case BeatmapSection.Events:
                        file.Events.AddRange(r.ReadEvents());
                        break;
                    default:
                        break;
                }
            }

            return file;
        }

        private static BeatmapSection? ReadUntilSectionStart(this StreamReader sr)
        {
            while (!sr.EndOfStream)
            {
                string str = sr.ReadLine();
                if (string.IsNullOrWhiteSpace(str) || !str.StartsWith("[")) continue;

                string stripped = str.TrimStart('[').TrimEnd(']');
                if (Enum.TryParse(stripped, out BeatmapSection a))
                    return a;
            }
            return null;
        }

        private static IEnumerable<EventBase> ReadEvents(this StreamReader sr)
        {
            string line;
            while (!string.IsNullOrEmpty(line = sr.ReadLine()) && !line.StartsWith("["))
            {
                if (!line.StartsWith("//") && !string.IsNullOrWhiteSpace(line))
                    yield return EventBase.FromString(line);
            }
        }

        public static string GetBackgroundPath(Beatmap map)
        {
            try
            {
                if (string.IsNullOrEmpty(map.Directory) || !Directory.Exists(map.Directory))
                    return null;

                string[] files = Directory.GetFiles(map.Directory, "*.osu");
                if (files.Length == 0) return null;

                using (var stream = File.OpenRead(files[0]))
                {
                    var beatmapFile = Read(stream);
                    var bgEvent = beatmapFile.Events.FirstOrDefault(e => e.GetType().Name.Contains("Background"));

                    if (bgEvent != null)
                    {
                        var prop = bgEvent.GetType().GetProperty("Filename") ?? bgEvent.GetType().GetProperty("Path");
                        string fileName = prop?.GetValue(bgEvent) as string;

                        if (!string.IsNullOrEmpty(fileName))
                        {
                            return Path.Combine(map.Directory, fileName.Trim('"'));
                        }
                    }
                }
            }
            catch { /* Silent fail for UI performance */ }
            return null;
        }
    }
}