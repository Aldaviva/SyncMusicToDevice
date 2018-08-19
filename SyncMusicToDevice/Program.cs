using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading.Tasks;
//using System.Windows.Forms;
using MediaDevices;
using SyncMusicToDevice.Service;
using TagLib;
using File = System.IO.File;

namespace SyncMusicToDevice
{
    public static class Program
    {
        private static readonly ISet<string>
            MusicFileExtensions = new HashSet<string>(new[] { ".mp3", ".flac", ".m4a", ".ogg", ".wma" });

//        [STAThread]
        public static async Task Main2()
        {
//            Application.EnableVisualStyles();
//            Application.SetCompatibleTextRenderingDefault(false);
//            Application.Run(new Form1());

            await TranscodeFile2();

/*            Stopwatch stopwatch = Stopwatch.StartNew();

            IList<MediaDevice> devices = MediaDevice.GetDevices().ToList();
            Console.WriteLine($"Found {devices.Count} connected MTP devices:");
            foreach (MediaDevice device in devices)
            {
                using (device)
                {
                    device.Connect();
                    Console.WriteLine(
                        $"{device.FriendlyName} ({device.Description}, {device.DeviceType}, {device.Manufacturer} {device.Model}, {device.Transport})");

                    IList<string> deviceMusicFiles = device.GetDirectoryInfo(@"\Internal shared storage\Music")
                        .EnumerateFiles("*", SearchOption.AllDirectories)
                        .Where(mediaFile =>
                        {
                            string extension = Path.GetExtension(mediaFile.Name)?.ToLower() ?? "";
                            return MusicFileExtensions.Contains(extension);
                        })
                        .Select(info => info.FullName)
                        .ToList();

                    foreach (string deviceMusicFile in deviceMusicFiles)
                    {
//                        Console.WriteLine(deviceMusicFile);
                    }

                    device.Disconnect();

                    stopwatch.Stop();

                    Console.WriteLine(
                        $"Found {deviceMusicFiles.Count:N0} music files on the device in {stopwatch.Elapsed} ({(double) deviceMusicFiles.Count / stopwatch.Elapsed.TotalSeconds:N} files/second)");
                }
            }

            stopwatch.Restart();
            IList<string> desktopMusicFiles = Directory.EnumerateFiles(@"D:\Music", "*", SearchOption.AllDirectories)
                .Where(filename =>
                {
                    string extension = Path.GetExtension(filename)?.ToLowerInvariant() ?? "";
                    return MusicFileExtensions.Contains(extension);
                }).ToList();

            int filesOverBitrateThreshold = 0;
            const int bitrateThreshold = 250;

            Console.WriteLine(
                $"Discovered {desktopMusicFiles.Count:N0} music files on the desktop in {stopwatch.Elapsed} seconds ({(double) desktopMusicFiles.Count / stopwatch.Elapsed.TotalSeconds:N} files/second)");
            foreach (string desktopMusicFile in desktopMusicFiles)
            {
                try
                {
                    using (TagLib.File metadata = TagLib.File.Create(desktopMusicFile))
                    using (MD5 md5 = MD5.Create())
                    using (FileStream fileStream = File.OpenRead(desktopMusicFile))
                    {
                        int bitrate = metadata.Properties.AudioBitrate;
                        var fileInfo = new FileInfo(desktopMusicFile);
                        //                        byte[] checksum = md5.ComputeHash(fileStream);
                        //                        string checksumString = BitConverter.ToString(checksum).Replace("-", "").ToLowerInvariant();
                        string checksumString = "";
//                        Console.WriteLine( $"{desktopMusicFile} ({bitrate} kbps, modified {fileInfo.LastWriteTimeUtc}, MD5 checksum {checksumString})");
                        if (bitrate > bitrateThreshold)
                        {
                            filesOverBitrateThreshold++;
                        }
                    }
                }
                catch (CorruptFileException)
                {
                    Console.WriteLine($"!! Corrupt file {desktopMusicFile}");
                }
                catch (UnsupportedFormatException)
                {
                    Console.WriteLine($"!! Unsupported format {desktopMusicFile}");
                }
            }

            Console.WriteLine(
                $"Found {desktopMusicFiles.Count:N0} music files on the desktop in {stopwatch.Elapsed} ({(double) desktopMusicFiles.Count / stopwatch.Elapsed.TotalSeconds:N} files/second)");

            Console.WriteLine(
                $"{filesOverBitrateThreshold:N0} out of {desktopMusicFiles.Count:N0} ({(double) filesOverBitrateThreshold / desktopMusicFiles.Count:P0}) are over {bitrateThreshold:N0} kbps and will be transcoded.");
                */
        }

        private static void TranscodeFile()
        {
            const string inputFile = @"c:\Users\Ben\Desktop\Bad Chemistry.flac";
            const string outputFile = @"c:\Users\Ben\Desktop\Bad Chemistry.m4a";
            const string errorsFile = @"c:\Users\Ben\Desktop\Bad Chemistry.log";

            using (TagLib.File metadata = TagLib.File.Create(inputFile))
            {
                TimeSpan duration = metadata.Properties.Duration;
                long inputFileSize = new FileInfo(inputFile).Length;
//                var converter = new DMCSCRIPTINGLib.Converter();

                string encoder = "m4a FDK (AAC)";
                // https://wiki.hydrogenaud.io/index.php?title=Fraunhofer_FDK_AAC#Usage_2
                string compressionSettings = @"-cli_cmd=""-m 4 -p 2 --ignorelength -S -o {qt}[outfile]{qt} - """;

                Stopwatch stopwatch = Stopwatch.StartNew();
//                converter.Convert(inputFile, outputFile, encoder, compressionSettings, errorsFile);
                stopwatch.Stop();

                double relativeSpeed = duration.TotalMilliseconds / stopwatch.Elapsed.TotalMilliseconds;
                long outputFileSize = new FileInfo(outputFile).Length;
                Console.WriteLine(
                    $"Converted {inputFile} to AAC-LC in {stopwatch.Elapsed.TotalSeconds:N} seconds\n{relativeSpeed:N}x speed\n{(double) outputFileSize / inputFileSize:P} file size\n{((double) inputFileSize - outputFileSize) / 1024 / 1024:N} MB saved");
                string errorText = File.ReadAllText(errorsFile);
                if (string.IsNullOrWhiteSpace(errorText))
                {
                    errorText = "no errors";
                }
                Console.WriteLine(errorText);
                File.Delete(errorsFile);
            }
        }

        private static async Task TranscodeFile2()
        {
            var transcodingService = new dbPowerampTranscodingService();
            const string source = @"D:\Music\Trance\Way Out West\Dancehall Tornado.flac";
            string destination = Path.ChangeExtension(Path.GetTempFileName(), "m4a");
            Console.WriteLine($"Converting {source} to {destination}");
            await transcodingService.Transcode(source, destination);
            Console.WriteLine("conversion done");
        }

        private static IEnumerable<MediaDirectoryInfo> EnumerateDirectoriesRecursively(MediaDirectoryInfo parentDirectory,
            int depth = 1)
        {
            IEnumerable<MediaDirectoryInfo> result = new List<MediaDirectoryInfo>();
            IEnumerable<MediaDirectoryInfo> childDirectories = parentDirectory.EnumerateDirectories();
            foreach (MediaDirectoryInfo childDirectory in childDirectories)
            {
                result = result.Append(childDirectory);
                if (depth > 1)
                {
                    result = result.Concat(EnumerateDirectoriesRecursively(childDirectory, depth - 1));
                }
            }

            return result;
        }
    }
}