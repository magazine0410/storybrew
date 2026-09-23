using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace StorybrewEditor.Util
{
    /// <summary>
    /// Native dialogs shown through kdialog or zenity.
    /// Methods block until the dialog is closed and return null when it is cancelled.
    /// </summary>
    public static class LinuxDialogs
    {
        private enum Tool { None, KDialog, Zenity }

        private static readonly Tool tool = findTool();

        public static string PickFolder(string title, string initialPath)
        {
            var arguments = new List<string>();
            switch (tool)
            {
                case Tool.KDialog:
                    addTitle(arguments, title);
                    arguments.Add("--getexistingdirectory");
                    if (!string.IsNullOrEmpty(initialPath)) arguments.Add(initialPath);
                    break;
                case Tool.Zenity:
                    arguments.AddRange(new[] { "--file-selection", "--directory" });
                    addTitle(arguments, title);
                    addZenityFilename(arguments, initialPath);
                    break;
            }
            return run(arguments);
        }

        public static string PickFile(string title, string initialPath, string filter)
        {
            var arguments = new List<string>();
            switch (tool)
            {
                case Tool.KDialog:
                    addTitle(arguments, title);
                    arguments.Add("--getopenfilename");
                    arguments.Add(string.IsNullOrEmpty(initialPath) ? "." : initialPath);
                    addKDialogFilter(arguments, filter);
                    break;
                case Tool.Zenity:
                    arguments.Add("--file-selection");
                    addTitle(arguments, title);
                    addZenityFilename(arguments, initialPath);
                    addZenityFilter(arguments, filter);
                    break;
            }
            return run(arguments);
        }

        public static string PickSaveLocation(string title, string initialPath, string extension, string filter)
        {
            var arguments = new List<string>();
            switch (tool)
            {
                case Tool.KDialog:
                    addTitle(arguments, title);
                    arguments.Add("--getsavefilename");
                    arguments.Add(string.IsNullOrEmpty(initialPath) ? "." : initialPath);
                    addKDialogFilter(arguments, filter);
                    break;
                case Tool.Zenity:
                    arguments.AddRange(new[] { "--file-selection", "--save" });
                    addTitle(arguments, title);
                    addZenityFilename(arguments, initialPath);
                    addZenityFilter(arguments, filter);
                    break;
            }

            var path = run(arguments);
            if (path != null && !string.IsNullOrEmpty(extension) && !Path.HasExtension(path))
                path += "." + extension.TrimStart('.');
            return path;
        }

        /// <summary>
        /// Returns true if the message was accepted.
        /// Returns false if no dialog tool is available.
        /// </summary>
        public static bool ShowMessage(string message, string title, bool okCancel)
        {
            var arguments = new List<string>();
            switch (tool)
            {
                case Tool.KDialog:
                    addTitle(arguments, title);
                    if (okCancel)
                        arguments.AddRange(new[] { "--yesno", message, "--yes-label", "OK", "--no-label", "Cancel" });
                    else arguments.AddRange(new[] { "--msgbox", message });
                    break;
                case Tool.Zenity:
                    arguments.AddRange(new[] { okCancel ? "--question" : "--info", "--no-markup", "--text", message });
                    addTitle(arguments, title);
                    if (okCancel) arguments.AddRange(new[] { "--ok-label", "OK", "--cancel-label", "Cancel" });
                    break;
                default:
                    return false;
            }

            try
            {
                return run(arguments) != null;
            }
            catch (Exception e)
            {
                Trace.WriteLine($"Failed to show message: {e}");
                return false;
            }
        }

        private static string run(List<string> arguments)
        {
            if (tool == Tool.None)
                throw new InvalidOperationException("No dialog tool found, please install kdialog or zenity");

            var startInfo = new ProcessStartInfo(tool == Tool.KDialog ? "kdialog" : "zenity")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            foreach (var argument in arguments)
                startInfo.ArgumentList.Add(argument);

            using (var process = Process.Start(startInfo))
            {
                var output = process.StandardOutput.ReadToEndAsync();
                var error = process.StandardError.ReadToEndAsync();
                process.WaitForExit();

                if (!string.IsNullOrWhiteSpace(error.Result))
                    Trace.WriteLine($"{startInfo.FileName}: {error.Result.TrimEnd()}");

                // Both tools exit with 1 when cancelled
                return process.ExitCode == 0 ? output.Result.TrimEnd('\n') : null;
            }
        }

        private static Tool findTool()
        {
            var desktop = Environment.GetEnvironmentVariable("XDG_CURRENT_DESKTOP") ?? string.Empty;
            var preferred = desktop.Contains("KDE", StringComparison.OrdinalIgnoreCase) ?
                new[] { Tool.KDialog, Tool.Zenity } : new[] { Tool.Zenity, Tool.KDialog };

            foreach (var candidate in preferred)
                if (isInPath(candidate == Tool.KDialog ? "kdialog" : "zenity"))
                    return candidate;

            Trace.WriteLine("No dialog tool found (kdialog or zenity)");
            return Tool.None;
        }

        private static bool isInPath(string name)
            => (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
                .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
                .Any(directory => File.Exists(Path.Combine(directory, name)));

        private static void addTitle(List<string> arguments, string title)
        {
            if (string.IsNullOrEmpty(title)) return;
            arguments.Add("--title");
            arguments.Add(title);
        }

        private static void addZenityFilename(List<string> arguments, string path)
        {
            if (string.IsNullOrEmpty(path)) return;

            // A trailing separator opens the folder instead of selecting it
            if (Directory.Exists(path) && !path.EndsWith(Path.DirectorySeparatorChar))
                path += Path.DirectorySeparatorChar;

            arguments.Add("--filename");
            arguments.Add(path);
        }

        private static void addKDialogFilter(List<string> arguments, string filter)
        {
            var filters = parseFilter(filter);
            if (filters.Count == 0) return;

            arguments.Add(string.Join("\n", filters.Select(f => $"{string.Join(" ", f.Patterns)}|{f.Name}")));
        }

        private static void addZenityFilter(List<string> arguments, string filter)
        {
            foreach (var f in parseFilter(filter))
            {
                arguments.Add("--file-filter");
                arguments.Add($"{f.Name} | {string.Join(" ", f.Patterns)}");
            }
        }

        /// <summary>
        /// Parses a Windows Forms filter, such as "Images (*.png, *.jpg)|*.png;*.jpg|All files (*.*)|*.*"
        /// </summary>
        private static List<(string Name, string[] Patterns)> parseFilter(string filter)
        {
            var filters = new List<(string, string[])>();
            if (string.IsNullOrEmpty(filter)) return filters;

            var parts = filter.Split('|');
            for (var i = 0; i + 1 < parts.Length; i += 2)
            {
                var patterns = parts[i + 1].Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(pattern => pattern == "*.*" ? "*" : pattern)
                    .ToArray();
                if (patterns.Length > 0)
                    filters.Add((parts[i].Trim(), patterns));
            }
            return filters;
        }
    }
}
