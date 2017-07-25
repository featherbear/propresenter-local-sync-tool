using System;
using System.Collections.Generic;
using System.IO;

namespace ProPresenter_Local_Sync_Tool
{
    internal class Utils
    {
        private const int BYTES_TO_READ = sizeof(long);

        public static bool StringIsTrue(string val)
        {
            return val == "true";
        }

        private static List<string> _CompareDirectory(string remoteDir, string localDir, List<string> ignore,
            List<string> conflict)
        {
            var result = new List<string>();
            foreach (var remotePath in Directory.GetFiles(remoteDir, "*.*",
                SearchOption.AllDirectories))
            {
                var relativepath = remotePath.Replace(remoteDir, "");
                var localPath = Path.Combine(localDir, relativepath);
                if (!ignore.Contains(relativepath))
                {
                    if (!File.Exists(localPath)) result.Add(relativepath);
                    // File exists only in first path
                    else if (FileDiff(remotePath, localPath))
                        conflict.Add((new FileInfo(remotePath).LastWriteTime > new FileInfo(localPath).LastWriteTime
                                         ? "_"
                                         : "") + relativepath);
                    ignore.Add(relativepath);
                }
            }
            return result;
        }

        // public static Tuple<List<string>, List<string>, List<string>> CompareDirectory(string remoteDir, string localDir,
        public static Dictionary<string, List<string>> CompareDirectory(string remoteDir, string localDir,
            bool recursive = true)
        {
            if (!remoteDir.EndsWith("\\")) remoteDir += "\\";
            if (!localDir.EndsWith("\\")) localDir += "\\";

            var ignore = new List<string>();
            var conflicts = new List<string>();

            var inRemote = _CompareDirectory(remoteDir, localDir, ignore, conflicts);
            var inLocal = _CompareDirectory(localDir, remoteDir, ignore, conflicts);

            return new Dictionary<string, List<string>>
            {
                {"new", inRemote},
                {"missing", inLocal},
                {"conflict", conflicts}
            };
        }

        public static bool FileDiff(string first, string second)
        {
            return FileDiff(new FileInfo(first), new FileInfo(second));
        }

        public static bool FileDiff(FileInfo first, FileInfo second)
        {
            // https://stackoverflow.com/a/1359947
            if (first.Length != second.Length || first.LastWriteTime != second.LastWriteTime)
                return true;

            if (first.FullName == second.FullName)
                return false;

            var iterations = (int)Math.Ceiling((double)first.Length / BYTES_TO_READ);

            using (var fs1 = first.OpenRead())
            using (var fs2 = second.OpenRead())
            {
                var one = new byte[BYTES_TO_READ];
                var two = new byte[BYTES_TO_READ];

                for (var i = 0; i < iterations; i++)
                {
                    fs1.Read(one, 0, BYTES_TO_READ);
                    fs2.Read(two, 0, BYTES_TO_READ);

                    if (BitConverter.ToInt64(one, 0) != BitConverter.ToInt64(two, 0))
                        return true;
                }
            }

            return false;
        }
    }
}