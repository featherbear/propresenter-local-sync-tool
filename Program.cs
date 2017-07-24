using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using CommandLine;
using CommandLine.Text;
using Microsoft.Win32;

namespace ProPresenter_Local_Sync_Tool
{
    internal class Program
    {
        private static bool stringIsTrue(string val)
        {
            return val == "true";
        }

        private static void Main(string[] args)
        {
            var parser = new ArgsParser();

            void Print(string str)
            {
                if (!parser.Quiet) Console.WriteLine(str);
            }

            var argsParser = new Parser(s =>
            {
                s.MutuallyExclusive = true;
                s.CaseSensitive = true;
                s.IgnoreUnknownArguments = false;
            });
            if (!argsParser.ParseArguments(args, parser))
            {
                Print(parser.GetUsage());
                Environment.Exit(0);
            }

            var registryKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Renewed Vision\\ProPresenter 6");
            if (registryKey == null)
            {
                Print("PP6 not installed");
                Environment.Exit(0);
            }
            // Is ProPresenter 6 installed?

            var appDataType = registryKey.GetValue("AppDataType");
            var appDataLocation = "";
            switch (appDataType)
            {
                case "OnlyThisUser":
                    // C:\Users\User\AppData\Roaming\RenewedVision\ProPresenter6\Preferences
                    appDataLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
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
                    Environment.Exit(-1);
                    // woah what, value doesn't exist?
                    break;
            }

            var syncPreferences = new XmlDocument();
            syncPreferences.Load(Path.Combine(appDataLocation, "Preferences\\SyncPreferences.pro6pref"));
            // CATCH System.IO.FileNotFoundException CATCH System.Xml.XmlException

            // ReSharper disable PossibleNullReferenceException
            var syncLibrary = parser.SyncLibrary || parser.SyncLibraryNo
                ? parser.SyncLibrary
                : stringIsTrue(syncPreferences["RVPreferencesSynchronization"]["SyncLibrary"].InnerText);
            var syncPlaylist = parser.SyncPlaylist || parser.SyncPlaylistNo
                ? parser.SyncPlaylist
                : stringIsTrue(syncPreferences["RVPreferencesSynchronization"]["SyncPlaylists"].InnerText);
            var syncTemplate = parser.SyncTemplate || parser.SyncTemplateNo
                ? parser.SyncTemplate
                : stringIsTrue(syncPreferences["RVPreferencesSynchronization"]["SyncTemplates"].InnerText);
            var syncMedia = parser.SyncMedia || parser.SyncMediaNo
                ? parser.SyncMedia
                : stringIsTrue(syncPreferences["RVPreferencesSynchronization"]["SyncMedia"].InnerText);
            var syncReplace = parser.SyncReplace || parser.SyncReplaceNo
                ? parser.SyncReplace
                : stringIsTrue(syncPreferences["RVPreferencesSynchronization"]["ReplaceFiles"].InnerText);
            var syncMode = parser.SyncDown || parser.SyncUp || parser.SyncBoth
                ? new List<bool>
                {
                    parser.SyncDown,
                    parser.SyncBoth,
                    parser.SyncUp
                }.IndexOf(true) - 1
                : new List<string>
                {
                    "UpdateClient",
                    "UpdateBoth",
                    "UpdateServer"
                }.IndexOf(
                    syncPreferences["RVPreferencesSynchronization"]["SyncMode"].InnerText) - 1;
            var dirSource = parser.SyncSource ?? syncPreferences["RVPreferencesSynchronization"]["Source"].InnerText;
            if (dirSource.Length == 0)
            {
                Print("Empty source");
                Environment.Exit(-2);
            }

            var generalPreferences = new XmlDocument();
            generalPreferences.Load(Path.Combine(appDataLocation, "Preferences\\GeneralPreferences.pro6pref"));
            var dirLibrary = generalPreferences["RVPreferencesGeneral"]["SelectedLibraryFolder"]["Location"].InnerText;
            var dirMedia = generalPreferences["RVPreferencesGeneral"]["MediaRepositoryPath"].InnerText;

            // Sync Media
            // server - Path.Combine(dirSource, "__Media");
            // __User_Data?
            // client - dirMedia;

            // Sync Playlist
            // server - Path.Combine(dirSource, "__Playlist_Data");
            // client - Path.Combine(appDataLocation, "PlaylistData");
            var ServerPlaylist = new XmlDocument();
            ServerPlaylist.PreserveWhitespace = true;
            ServerPlaylist.Load(Path.Combine(dirSource, "__Playlist_Data\\Default.pro6pl"));
            foreach (XmlNode item in ServerPlaylist.GetElementsByTagName("RVDocumentCue"))
                item.Attributes["filePath"].Value = Uri.EscapeDataString(dirLibrary) + item.Attributes["filePath"].Value
                                                        .Split(new[] { "%5C" }, StringSplitOptions.None).Reverse()
                                                        .ToArray()[0];
            ServerPlaylist.Save(Path.Combine(appDataLocation, "PlaylistData\\Default.pro6pl"));
            // '\' already included

            // Sync Documents
            // server - Path.Combine(dirSource, "__Documents");
            // client - dirLibrary

            // Sync Templates
            // server - Path.Combine(dirSource, "__Templates");
            // client - Path.Combine(appDataLocation, "Templates");

            /*
             * __Documents
             * __Playlist_Data --> Default.pro6pl :RVDocumentCue D%3A%5CUsers%5CAndrew%5CDocuments%5CProPresenter6%5C
             * __Templates
             * __User_Data
             */

            // ReSharper restore PossibleNullReferenceException

            /*
             *
             * LabelsPreferences.pro6pref
             */
        }

        private class ArgsParser
        {
            [Option('d', "down", HelpText = "Download files to the repository", MutuallyExclusiveSet = "syncMode")]
            public bool SyncDown { get; set; }

            [Option('b', "both", HelpText = "Synchronise files to and from the repository",
                MutuallyExclusiveSet = "syncMode")]
            public bool SyncBoth { get; set; }

            [Option('u', "up", HelpText = "Upload files to the repository", MutuallyExclusiveSet = "syncMode")]
            public bool SyncUp { get; set; }

            [Option('m', "media", HelpText = "Sync media", MutuallyExclusiveSet = "syncMedia")]
            public bool SyncMedia { get; set; }

            [Option('M', "no-media", HelpText = "Do not sync media", MutuallyExclusiveSet = "syncMedia")]
            public bool SyncMediaNo { get; set; }

            [Option('l', "library", HelpText = "Sync library", MutuallyExclusiveSet = "syncLibrary")]
            public bool SyncLibrary { get; set; }

            [Option('L', "no-library", HelpText = "Do not sync library", MutuallyExclusiveSet = "syncLibrary")]
            public bool SyncLibraryNo { get; set; }

            [Option('t', "template", HelpText = "Sync templates", MutuallyExclusiveSet = "syncTemplate")]
            public bool SyncTemplate { get; set; }

            [Option('T', "no-template", HelpText = "Do not sync templates", MutuallyExclusiveSet = "syncTemplate")]
            public bool SyncTemplateNo { get; set; }

            [Option('p', "playlist", HelpText = "Sync playlists", MutuallyExclusiveSet = "syncPlaylist")]
            public bool SyncPlaylist { get; set; }

            [Option('P', "no-playlist", HelpText = "Do not sync playlists", MutuallyExclusiveSet = "syncPlaylist")]
            public bool SyncPlaylistNo { get; set; }

            [Option('r', "replace", HelpText = "Replace files", MutuallyExclusiveSet = "syncReplace")]
            public bool SyncReplace { get; set; }

            [Option('R', "no-replace", HelpText = "Do not replace files", MutuallyExclusiveSet = "syncReplace")]
            public bool SyncReplaceNo { get; set; }

            [Option('s', "source",
                HelpText = "ProPresenter local sync source")]
            public string SyncSource { get; set; }

            [Option('q', "quiet",
                HelpText = "Suppresses output messages")]
            public bool Quiet { get; set; }

            [HelpOption('h', "help")]
            public string GetUsage()
            {
                return HelpText.AutoBuild(this,
                    current => HelpText.DefaultParsingErrorsHandler(this, current));
            }
        }
    }
}