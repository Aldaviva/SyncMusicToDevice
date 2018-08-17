using System;
using SQLite;

namespace SyncMusicToDevice.Data
{
    public class SynchronizedMusicFile
    {
        /// <summary>
        /// The path of the music file on the desktop computer.
        /// This includes the filename.
        /// This path is relative to the root music directory.
        /// </summary>
        /// <example>
        /// Big Beat\Andy Hunter\Alive.mp3
        /// </example>
        [PrimaryKey]
        public string DesktopFilePath { get; set; }

        /// <summary>
        /// The last-modified date of the music file on the desktop, as a <see cref="DateTime"/> in UTC.
        /// </summary>
        public DateTime Modified { get; set; }

        /// <summary>
        /// The checksum of the music file on the desktop, calculated using an <c>MD5</c> hash of the file's bytes.
        /// </summary>
        public byte[] Checksum { get; set; }

        public string DeviceFileName { get; set; }
    }
}