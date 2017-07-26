using CommandLine;
using CommandLine.Text;

namespace ProPresenterLocalSyncTool
{
    internal class CommandLineArguments
    {
        [Option('d', "down", HelpText = "Download files from the sync source", MutuallyExclusiveSet = "syncMode")]
        public bool SyncDown { get; set; }

        [Option('b', "both", HelpText = "Synchronise files to and from the sync source",
            MutuallyExclusiveSet = "syncMode")]
        public bool SyncBoth { get; set; }

        [Option('u', "up", HelpText = "Upload files to the sync source", MutuallyExclusiveSet = "syncMode")]
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