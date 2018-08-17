using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using NLog;
using SyncMusicToDevice.Injection;
using TagLib;
using File = System.IO.File;

namespace SyncMusicToDevice.Service
{
    [Component]
    public class DesktopMusicServiceImpl : DesktopMusicService
    {
        private static readonly Logger LOGGER = LogManager.GetCurrentClassLogger();

        private static readonly ISet<string> MusicFileExtensions =
            new HashSet<string>(new[] { ".mp3", ".flac", ".m4a", ".ogg", ".wma" });

        public IEnumerable<string> ListFiles(string rootDirectory)
        {
            return Directory.EnumerateFiles(rootDirectory, "*", SearchOption.AllDirectories).AsParallel()
                .Where(filename =>
                {
                    string extension = Path.GetExtension(filename)?.ToLowerInvariant() ?? "";
                    return MusicFileExtensions.Contains(extension);
                });
        }

        public DateTime GetDateModified(string filePath)
        {
            return new FileInfo(filePath).LastWriteTimeUtc;
        }

        public Task<byte[]> GetChecksum(string filePath)
        {
            return Task.Run(() =>
            {
                LOGGER.Trace("Calculating MD5 hash of {0}", filePath);
                using (MD5 md5 = MD5.Create())
                using (FileStream fileStream = File.OpenRead(filePath))
                {
                    return md5.ComputeHash(fileStream);
                }
            });
        }

        public Task<int> GetBitrate(string filePath)
        {
            return Task.Run(() =>
            {
                try
                {
                    using (TagLib.File metadata = TagLib.File.Create(filePath))
                    {
                        return metadata.Properties.AudioBitrate;
                    }
                }
                catch (Exception e) when (e is UnsupportedFormatException || e is CorruptFileException)
                {
                    LOGGER.Error("Encountered unreadable file while trying to get bitrate: {0} ({1})", e.Message, filePath);
                    throw;
                }
            });
        }
    }
}