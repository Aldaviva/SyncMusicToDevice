using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using NLog;
using SyncMusicToDevice.Injection;

namespace SyncMusicToDevice.Service
{
    /**
     * COM add-in seems to be single-threaded, but forking processes is parallelizable
     */
    [Component]
    // ReSharper disable once InconsistentNaming
#pragma warning disable IDE1006 // Naming Styles
    public class dbPowerampTranscodingService : TranscodingService
#pragma warning restore IDE1006 // Naming Styles
    {
        private static readonly Logger LOGGER = LogManager.GetCurrentClassLogger();

        private const string EncoderName = @"m4a FDK (AAC)";
        private const string CompressionSettings = @"-cli_cmd=""-m 4 -p 2 --ignorelength -S -o {qt}[outfile]{qt} - """;
        private const string CoreConverterPath = @"c:\Programs\Multimedia\dBpoweramp\CoreConverter.exe";

        public Task Transcode(string sourceFile, string destinationFile)
        {
            return Task.Run(() =>
            {
                LOGGER.Debug($"Transcoding {sourceFile} to AAC...");

                var p = new ProcessStartInfo(CoreConverterPath,
                    $"-infile=\"{sourceFile}\" " +
                    $"-outfile=\"{destinationFile}\" " +
                    $"-convert_to=\"{EncoderName}\" " +
                    $"{CompressionSettings}")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                Process process = Process.Start(p);
                process?.WaitForExit();
            });
        }
    }
}