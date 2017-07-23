using System;
using System.IO;
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
            var options = new Options();
            var parser = new Parser(s =>
                {
                    s.MutuallyExclusive = true;
                    s.CaseSensitive = true;
                    s.IgnoreUnknownArguments = false;
                }
            );

            //if (parser.ParseArguments(args, options))
            //  if (options.SyncDown) Console.WriteLine("");
            //if (options.Verbose) Console.WriteLine("Filename: {0}", options.InputFile);

            var registryKey = Registry.CurrentUser.OpenSubKey("Software\\Renewed Vision\\ProPresenter 6");
            if (registryKey == null) Environment.Exit(0);
            // Is ProPresenter 6 installed?

            var appDataType = registryKey.GetValue("AppDataType");
            var appDataLocation = "";
            switch (appDataType)
            {
                case "OnlyThisUser":
                    // C:\Users\User\AppData\Roaming\RenewedVision\ProPresenter6\Preferences
                    appDataLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "RenewedVision/ProPresenter6");
                    break;

                case "ForAllUsers":
                    // C:\ProgramData\RenewedVision\ProPresenter6\Preferences
                    // C:\Users\Users\AppData\Local\VirtualStore\ProgramData\RenewedVision\ProPresenter6\Preferences
                    appDataLocation =
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                            "RenewedVision/ProPresenter6");
                    break;

                case "CustomPath":
                    appDataLocation = registryKey.GetValue("AppDataLocation").ToString();
                    break;

                case null:
                    // woah what, value doesn't exist?
                    break;
            }

            var syncPreferences = new XmlDocument();
            syncPreferences.Load(Path.Combine(appDataLocation, "Preferences\\SyncPreferences.pro6pref"));
            // CATCH System.IO.FileNotFoundException
            // CATCH System.Xml.XmlException

            // ReSharper disable PossibleNullReferenceException
            // TODO REORDER ME
            var boolTemplates = stringIsTrue(syncPreferences["RVPreferencesSynchronization"]["SyncTemplates"]
                .InnerText);
            var boolPlaylists = stringIsTrue(syncPreferences["RVPreferencesSynchronization"]["SyncPlaylists"]
                .InnerText);
            var boolLibrary = stringIsTrue(syncPreferences["RVPreferencesSynchronization"]["SyncLibrary"].InnerText);
            var boolMedia = stringIsTrue(syncPreferences["RVPreferencesSynchronization"]["SyncMedia"].InnerText);
            var boolReplace = stringIsTrue(syncPreferences["RVPreferencesSynchronization"]["ReplaceFiles"].InnerText);
            var syncMode = syncPreferences["RVPreferencesSynchronization"]["SyncMode"].InnerText;
            var dirSource = syncPreferences["RVPreferencesSynchronization"]["Source"].InnerText;
            /*
             * __Documents
             * __Playlist_Data --> Default.pro6pl :RVDocumentCue D%3A%5CUsers%5CAndrew%5CDocuments%5CProPresenter6%5C
             * __Templates
             * __User_Data
             */
            var generalPreferences = new XmlDocument();
            generalPreferences.Load(Path.Combine(appDataLocation, "Preferences\\GeneralPreferences.pro6pref"));
            var dirLibrary = syncPreferences["RVPreferencesGeneral"]["SelectedLibraryFolder"]["Location"].InnerText;
            var dirMedia = syncPreferences["RVPreferencesGeneral"]["MediaRepositoryPath"].InnerText;
            // ReSharper restore PossibleNullReferenceException

            //

            /*
             *
             * LabelsPreferences.pro6pref
             */
        }

        private class Options
        {
            [Option('D', "down", HelpText = "Download files to the repository", MutuallyExclusiveSet = "syncDown")]
            public bool SyncDown { get; set; }

            [Option('B', "both", HelpText = "Synchronise files to and from the repository",
                MutuallyExclusiveSet = "syncBoth")]
            public bool SyncBoth { get; set; }

            [Option('U', "up", HelpText = "Upload files to the repository", MutuallyExclusiveSet = "syncUp")]
            public bool SyncUp { get; set; }

            [Option('p', "path", Required = true,
                HelpText = "ProPresenter local sync directory")]
            public string InputFile { get; set; }

            [Option('s', "silent",
                HelpText = "Suppresses output messages")]
            public bool Silent { get; set; }

            [ParserState]
            public IParserState LastParserState { get; set; }

            [HelpOption]
            public string GetUsage()
            {
                return HelpText.AutoBuild(this,
                    current => HelpText.DefaultParsingErrorsHandler(this, current));
            }
        }
    }
}