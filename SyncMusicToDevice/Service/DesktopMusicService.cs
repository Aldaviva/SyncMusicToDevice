using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SyncMusicToDevice.Service
{
    public interface DesktopMusicService
    {
        /// <summary>
        /// Find all the music files in a given folder and its subfolders on the desktop computer. Music files are matched by file extension.
        /// </summary>
        /// <param name="rootDirectory">The topmost folder in which to search for music files</param>
        /// <example>D:\Music</example>
        /// <returns>Collection of absolute filenames of music files.</returns>
        IEnumerable<string> ListFiles(string rootDirectory);

        /// <returns> /// The last-modified date of the music file on the desktop, as a <see cref="DateTime"/> in UTC. /// </returns>
        DateTime GetDateModified(string filePath);

        /// <returns>A <see cref="Task"/> that will return the checksum of the music file on the desktop, calculated using an <c>MD5</c> hash of the file's bytes.</returns>
        Task<byte[]> GetChecksum(string filePath);

        /// <summary>
        /// Get the bitrate of the given audio file. CBR files return the constant bitrate, while VBR files return the average or nominal rate.
        /// </summary>
        /// <param name="filePath">Absolute path of file to read</param>
        /// <returns>A <see cref="Task"/> that will return the given file's bitrate in kilobits per second.</returns>
        Task<int> GetBitrate(string filePath);
    }
}