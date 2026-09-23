using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace StorybrewEditor.Util
{
    public static class OsuHelper
    {
        public static string GetOsuExePath()
        {
            if (!OperatingSystem.IsWindows())
                return getWineOsuExePath();

            try
            {
                using (var registryKey = Registry.ClassesRoot.OpenSubKey("osu\\DefaultIcon"))
                    if (registryKey != null)
                    {
                        var value = registryKey.GetValue(null).ToString();
                        var startIndex = value.IndexOf("\"");
                        var endIndex = value.LastIndexOf("\"");
                        return value.Substring(startIndex + 1, endIndex - 1);
                    }
            }
            catch
            {
                // ArgumentOutOfRangeException can happen here from "registryKey.GetValue(null).ToString()"
            }

            // Default stable install path
            var defaultPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "osu!", "osu!.exe");
            if (File.Exists(defaultPath))
                return defaultPath;

            return string.Empty;
        }

        private static string getWineOsuExePath()
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var user = Environment.UserName;

            var prefixes = new List<string>();
            var winePrefix = Environment.GetEnvironmentVariable("WINEPREFIX");
            if (!string.IsNullOrEmpty(winePrefix)) prefixes.Add(winePrefix);
            prefixes.Add(Path.Combine(home, ".wine"));

            var candidates = new List<string>()
            {
                // osu-winello
                Path.Combine(home, ".local", "share", "osu-wine", "osu!", "osu!.exe"),
            };
            foreach (var prefix in prefixes)
            {
                candidates.Add(Path.Combine(prefix, "drive_c", "users", user, "AppData", "Local", "osu!", "osu!.exe"));
                candidates.Add(Path.Combine(prefix, "drive_c", "osu!", "osu!.exe"));
            }

            return candidates.FirstOrDefault(File.Exists) ?? string.Empty;
        }

        private static string getDefaultFolder()
            => OperatingSystem.IsWindows() ?
                Path.GetPathRoot(Environment.CurrentDirectory) :
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        public static string GetOsuFolder()
        {
            var osuPath = GetOsuExePath();
            if (string.IsNullOrEmpty(osuPath))
                return getDefaultFolder();

            return Path.GetDirectoryName(osuPath);
        }

        public static string GetOsuSongFolder()
        {
            var osuPath = GetOsuExePath();
            if (string.IsNullOrEmpty(osuPath))
                return getDefaultFolder();

            var osuFolder = Path.GetDirectoryName(osuPath);
            var songsFolder = Path.Combine(osuFolder, "Songs");
            return Directory.Exists(songsFolder) ? songsFolder : osuFolder;
        }
    }
}
