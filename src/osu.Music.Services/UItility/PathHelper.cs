using Microsoft.Win32;
using System;
using System.IO;

namespace Osu.Music.Services.UItility
{
    public static class PathHelper
    {
        public static string GetOsuInstallationFolder()
        {
            string[] registryKeys =
            {
                @"HKEY_CLASSES_ROOT\osu\shell\open\command",
                @"HKEY_CLASSES_ROOT\osu!\shell\open\command"
            };

            foreach (var key in registryKeys)
            {
                var value = Registry.GetValue(key, string.Empty, null) as string;

                if (string.IsNullOrWhiteSpace(value))
                    continue;

                try
                {
                    // value example:
                    // "C:\Users\User\AppData\Local\osu!\osu!.exe" "%1"
                    value = value.Trim();

                    if (value.StartsWith("\""))
                        value = value.Substring(1);

                    value = value.Split('\"')[0];

                    var dir = Path.GetDirectoryName(value);
                    if (Directory.Exists(dir))
                        return dir;
                }
                catch
                {
                    // ignore malformed registry values
                }
            }

            string[] fallbackPaths =
            {
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "osu!"
                ),
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "osu!"
                ),
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    "osu!"
                )
            };

            foreach (var path in fallbackPaths)
            {
                if (Directory.Exists(path))
                    return path;
            }
            return string.Empty;
        }
    }
}
