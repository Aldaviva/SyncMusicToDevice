using System;
using System.IO;

namespace SyncMusicToDevice.Native
{
    /// <summary>
    /// Alternative Managed API
    /// https://pinvoke.net/default.aspx/shlwapi/PathRelativePathTo.html
    /// </summary>
    internal static class PathRelativePathTo
    {
        public static string GetRelativePath(FileSystemInfo parent, FileSystemInfo child)
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            if (child == null) throw new ArgumentNullException(nameof(child));

            string path1FullName = GetFullName(parent);
            string path2FullName = GetFullName(child);

            var uri1 = new Uri(path1FullName);
            var uri2 = new Uri(path2FullName);
            Uri relativeUri = uri1.MakeRelativeUri(uri2);

            return Uri.UnescapeDataString(relativeUri.OriginalString);
        }

        private static string GetFullName(FileSystemInfo path)
        {
            string fullName = path.FullName;

            if (path is DirectoryInfo)
            {
                if (fullName[fullName.Length - 1] != Path.DirectorySeparatorChar)
                {
                    fullName += Path.DirectorySeparatorChar;
                }
            }

            return fullName;
        }
    }
}