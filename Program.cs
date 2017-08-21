using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;
using CommandLine;
using Microsoft.Win32;

namespace ProPresenterLocalSyncTool
{
    internal class Program
    {
        private static bool _quiet;
        private static int _syncMode;
        private static bool _syncReplace;

        public static void Print(string str)
        {
            if (!_quiet) Console.WriteLine(str);
        }

        private static void Main(string[] sysargs)
        {
            var versionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly().Location);

            Console.Title = versionInfo.ProductName;

            var args = new CommandLineArguments();
            var argsParser = new Parser(s =>
            {
                s.MutuallyExclusive = true;
                s.CaseSensitive = true;
                s.IgnoreUnknownArguments = false;
            });
            if (!argsParser.ParseArguments(sysargs, args))
            {
                Console.WriteLine(args.GetUsage());
                Environment.Exit(0);
            }

            _quiet = args.Quiet;
            if (args.Update)
                using (var webClient = new WebClient())
                {
                    Print("Application update requested");
                    webClient.Headers.Add("user-agent", "ProPresenter Sync Tool");
                    var json = webClient.DownloadString(
                        "https://api.github.com/repos/bearbear12345/propresenter-local-sync-tool/releases/latest");
                    if (new Version(versionInfo.ProductVersion).CompareTo(new Version(Regex
                            .Match(json, "\"tag_name\":\"(.+?)\"").Groups[1].Value)) > 0)
                    {
                        var filePath = Process.GetCurrentProcess().MainModule.FileName;
                        webClient.DownloadFile(Regex.Match(json, "\"browser_download_url\":\"(.+?)\"").Groups[1].Value,
                            filePath + ".update");
                        File.Move(filePath, filePath + ".old");
                        File.Move(filePath + ".update", filePath);
                    }
                    Print("Application is up to date");
                }

            Print(versionInfo.ProductName + " v" + versionInfo.ProductVersion + Environment.NewLine +
                  versionInfo.LegalCopyright + Environment.NewLine);
            var registryKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Renewed Vision\\ProPresenter 6");
            if (registryKey == null)
            {
                Print("FATAL: ProPresenter 6 not installed");
                Environment.Exit(-1);
            }
            // Is ProPresenter 6 installed?
            var appDataType = registryKey.GetValue("AppDataType");
            var appDataLocation = "";
            switch (appDataType)
            {
                case "OnlyThisUser":
                    // C:\Users\User\AppData\Roaming\RenewedVision\ProPresenter6\Preferences
                    appDataLocation = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "RenewedVision\\ProPresenter6");
                    break;

                case "ForAllUsers":
                    // C:\ProgramData\RenewedVision\ProPresenter6\Preferences C:\Users\Users\AppData\Local\VirtualStore\ProgramData\RenewedVision\ProPresenter6\Preferences
                    appDataLocation =
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                            "RenewedVision\\ProPresenter6");
                    break;

                case "CustomPath":
                    appDataLocation = registryKey.GetValue("AppDataLocation").ToString();
                    break;

                case null:
                    Print("FATAL: appDataType = " + appDataType);
                    Environment.Exit(999);
                    // woah what, value doesn't exist?
                    break;
            }

            // Print("Application data found in " + appDataLocation + Environment.NewLine);

            var syncPreferences = new XmlDocument();
            var generalPreferences = new XmlDocument();
            try
            {
                syncPreferences.Load(Path.Combine(appDataLocation, "Preferences\\SyncPreferences.pro6pref"));
                generalPreferences.Load(Path.Combine(appDataLocation, "Preferences\\GeneralPreferences.pro6pref"));
            }
            catch (FileNotFoundException)
            {
                Print(
                    "FATAL: Files are missing are inaccessible. Please open ProPresenter 6 and save settings at least once");
                Environment.Exit(-1);
            }
            // CATCH System.IO.FileNotFoundException CATCH System.Xml.XmlException

            // ReSharper disable PossibleNullReferenceException
            var dirSource = args.SyncSource ?? syncPreferences["RVPreferencesSynchronization"]["Source"].InnerText;
            if (dirSource.Length == 0)
            {
                Print("Error: Sync source not specified");
                Environment.Exit(-2);
            }
            if (!dirSource.EndsWith("\\")) dirSource += "\\";

            var syncLibrary = args.SyncLibrary || args.SyncLibraryNo
                ? args.SyncLibrary
                : Utils.StringIsTrue(syncPreferences["RVPreferencesSynchronization"]["SyncLibrary"].InnerText);
            var syncPlaylist = args.SyncPlaylist || args.SyncPlaylistNo
                ? args.SyncPlaylist
                : Utils.StringIsTrue(syncPreferences["RVPreferencesSynchronization"]["SyncPlaylists"].InnerText);
            var syncTemplate = args.SyncTemplate || args.SyncTemplateNo
                ? args.SyncTemplate
                : Utils.StringIsTrue(syncPreferences["RVPreferencesSynchronization"]["SyncTemplates"].InnerText);
            var syncMedia = args.SyncMedia || args.SyncMediaNo
                ? args.SyncMedia
                : Utils.StringIsTrue(syncPreferences["RVPreferencesSynchronization"]["SyncMedia"].InnerText);
            _syncReplace = args.SyncReplace || args.SyncReplaceNo
                ? args.SyncReplace
                : Utils.StringIsTrue(syncPreferences["RVPreferencesSynchronization"]["ReplaceFiles"].InnerText);
            _syncMode = args.SyncDown || args.SyncUp || args.SyncBoth
                ? new List<bool>
                {
                    args.SyncDown,
                    args.SyncBoth,
                    args.SyncUp
                }.IndexOf(true) - 1
                : new List<string>
                {
                    "UpdateClient",
                    "UpdateBoth",
                    "UpdateServer"
                }.IndexOf(
                    syncPreferences["RVPreferencesSynchronization"]["SyncMode"].InnerText) - 1;
            Print("Sync Mode: " + new List<string> { "Down", "Both", "Up" }[_syncMode + 1]);
            if (_syncMode != 0) Print("Sync Replace: " + (_syncReplace ? "Yes" : "No"));
            Print("Library: " + (syncLibrary ? "Yes" : "No"));
            Print("Playlist: " + (syncPlaylist ? "Yes" : "No"));
            Print("Templates: " + (syncTemplate ? "Yes" : "No"));
            Print("Media: " + (syncMedia ? "Yes" : "No"));

            var remoteLibrary = Path.Combine(dirSource, "__Documents\\Default");
            var remoteTemplate = Path.Combine(dirSource, "__Templates");
            var remoteMedia = Path.Combine(dirSource, "__Media");
            var remotePlaylist = Path.Combine(dirSource, "__Playlist_Data");

            var localLibrary = generalPreferences["RVPreferencesGeneral"]["SelectedLibraryFolder"]["Location"]
                .InnerText;
            var localTemplate = Path.Combine(appDataLocation, "Templates");
            var localMedia = generalPreferences["RVPreferencesGeneral"]["MediaRepositoryPath"].InnerText;
            var localPlaylist = Path.Combine(appDataLocation, "PlaylistData");

            // ReSharper enable PossibleNullReferenceException

            if (syncLibrary)
            {
                Print(Environment.NewLine + "Syncing library");
                SynchroniseWithRemote(remoteLibrary, localLibrary);
            }

            if (syncTemplate)
            {
                Print(Environment.NewLine + "Syncing templates");
                SynchroniseWithRemote(remoteTemplate, localTemplate);
            }

            if (syncMedia)
            {
                Print(Environment.NewLine + "Syncing media");
                SynchroniseWithRemote(remoteMedia, localMedia);
            }

            if (syncPlaylist)
            {
                Print(Environment.NewLine + "Syncing playlist");

                var compare = Utils.CompareDirectory(remotePlaylist, localPlaylist, false, true);
                Directory.CreateDirectory(remotePlaylist);

                if (_syncMode != 1)

                    foreach (var file in compare["new"])
                    {
                        Print("  Receiving " + file);

                        var playlist = new XmlDocument { PreserveWhitespace = true };
                        var sourceFile = Path.Combine(remotePlaylist, file);
                        var targetFile = Path.Combine(localPlaylist, file);
                        playlist.Load(sourceFile);
                        foreach (XmlNode item in playlist.GetElementsByTagName("RVDocumentCue"))
                            item.Attributes["filePath"].Value = Uri.EscapeDataString(localLibrary) +
                                                                item.Attributes["filePath"].Value.Split(new[] { "%5C" },
                                                                        StringSplitOptions.None)
                                                                    .Reverse().ToArray()[0];
                        playlist.Save(targetFile);
                        Utils.MirrorTimestamps(sourceFile, targetFile);
                    }

                if (_syncMode != -1)
                    foreach (var file in compare["missing"])
                    {
                        Print("  Uploading " + file);
                        Utils.CopyClone(Path.Combine(remotePlaylist, file), Path.Combine(localPlaylist, file),
                            _syncReplace);
                    }
                if (_syncMode == 0)
            }
            /*
             * LabelsPreferences.pro6pref
             */

            Print(Environment.NewLine + "Sync complete!");
            var query = string.Format("SELECT ParentProcessId FROM Win32_Process WHERE ProcessId = " +
                                      Process.GetCurrentProcess().Id);
            var results = new ManagementObjectSearcher("root\\CIMV2", query).Get().GetEnumerator();
            results.MoveNext();
            if (Process.GetProcessById((int)(uint)results.Current["ParentProcessId"]).ProcessName == "explorer")
            {
                Console.WriteLine("Press a key to exit");
                Console.ReadKey(true);
            }
        }

        private static void SynchroniseWithRemote(string remoteDir, string localDir)
        {
            Directory.CreateDirectory(remoteDir);
            Directory.CreateDirectory(localDir);
            var compare = Utils.CompareDirectory(remoteDir, localDir);
            if (_syncMode != 1)
                foreach (var file in compare["new"])
                {
                    Print("  Receiving " + file);

                    Directory.CreateDirectory(Path.Combine(localDir, Path.GetDirectoryName(file)));
                    Utils.CopyClone(Path.Combine(remoteDir, file), Path.Combine(localDir, file),
                        _syncReplace);
                }
            if (_syncMode != -1)
                foreach (var file in compare["missing"])
                {
                    Print("  Uploading " + file);

                    Directory.CreateDirectory(Path.Combine(remoteDir, Path.GetDirectoryName(file)));
                    Utils.CopyClone(Path.Combine(localDir, file), Path.Combine(remoteDir, file),
                        _syncReplace);
                }
            if (_syncMode == 0)
                foreach (var cfile in compare["conflict"])
                {
                    var remoteNewer = cfile[0] == '_';
                    var file = remoteNewer ? cfile.Substring(1) : cfile;
                    Print("  " + (remoteNewer ? "Receiving" : "Uploading") + " " + file);
                    Utils.CopyClone(Path.Combine(remoteNewer ? remoteDir : localDir, file),
                        Path.Combine(remoteNewer ? localDir : remoteDir, file),
                        true);
                }
        }
    }
}