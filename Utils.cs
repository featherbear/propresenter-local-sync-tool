using System;
using System.Collections.Generic;
using System.IO;

namespace ProPresenterLocalSyncTool
{
    internal class Utils
    {
        private const int BYTES_TO_READ = sizeof(long);

        public static Dictionary<string, List<string>> CompareDirectory(string remoteDir, string localDir,
            bool recursive = true, bool noConflictDirection = false)
        {
            if (!remoteDir.EndsWith("\\")) remoteDir += "\\";
            if (!localDir.EndsWith("\\")) localDir += "\\";

            var ignore = new List<string>();
            var conflicts = new List<string>();

            var inRemote = _CompareDirectory(remoteDir, localDir, ignore, conflicts, noConflictDirection);
            var inLocal = _CompareDirectory(localDir, remoteDir, ignore, conflicts, noConflictDirection);

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

        public static bool StringIsTrue(string val)
        {
            return val == "true";
        }

        private static List<string> _CompareDirectory(string remoteDir, string localDir, List<string> ignore,
            List<string> conflict, bool noConflictDirection = false)
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
                        conflict.Add(noConflictDirection
                            ? relativepath
                            : (new FileInfo(remotePath).LastWriteTime > new FileInfo(localPath).LastWriteTime
                                  ? "/"
                                  : "") + relativepath);
                    ignore.Add(relativepath);
                }
            }
            return result;
        }

        public static void CopyClone(string src, string dest, bool replace = false)
        {
            var existed = File.Exists(dest);
            File.Copy(src, dest, replace);
            if (!existed || replace)
                MirrorTimestamps(src, dest);
        }

        public static void MirrorTimestamps(string src, string dest)
        {
            File.SetCreationTime(dest, File.GetCreationTime(src));
            File.SetLastWriteTime(dest, File.GetLastWriteTime(src));
            File.SetLastAccessTime(dest, File.GetLastAccessTime(src));
        }
    }
}