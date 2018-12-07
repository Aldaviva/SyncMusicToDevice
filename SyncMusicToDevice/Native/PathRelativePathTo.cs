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
        /// <summary>
        /// Get the path of <c>child</c> relative to <c>parent</c>.
        /// </summary>
        /// <param name="parent">path to evaluate <c>child</c> relative to</param>
        /// <param name="child">path you want the return value to refer to</param>
        /// <returns>A path that points to the same file/directory as <c>child</c>, except that the path will be relative to <c>parent</c>.</returns>
        /// <example>
        /// GetRelativePath(new DirectoryInfo("c:/users/ben/"), new FileInfo("c:/users/ben/my music/song.mp3")) == "my music/song.mp3"
        /// GetRelativePath(new DirectoryInfo("c:/users/ben/desktop"), new FileInfo("c:/users/ben/my music/song.mp3")) == "../my music/song.mp3"
        /// </example>
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