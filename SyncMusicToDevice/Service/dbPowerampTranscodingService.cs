using System.Diagnostics;
using System.Threading.Tasks;
using NLog;
using SyncMusicToDevice.Injection;

namespace SyncMusicToDevice.Service
{
    /**
     * COM add-in seems to be single-threaded, but forking processes is parallelizable
     *
     * fdkaac arguments: https://wiki.hydrogenaud.io/index.php?title=Fraunhofer_FDK_AAC#fdkaac
     * To see arguments sent by dBpoweramp UI, use Process Monitor to trace Process Start event on fdkaac.exe
     *
     * -m 5 means use the highest VBR quality, Quality 5 (estimated bit rate: 224 kbps) [although it seems to actually be more like 192 kbps]
     * -p 2 means use AAC-LC (Low Complexity) instead of HE-AAC or HE-AACv2 which are for lower bitrates
     */
    [Component]
    // ReSharper disable once InconsistentNaming
#pragma warning disable IDE1006 // Naming Styles
    public class dbPowerampTranscodingService : TranscodingService
#pragma warning restore IDE1006 // Naming Styles
    {
        private static readonly Logger LOGGER = LogManager.GetCurrentClassLogger();

        private const string EncoderName = @"m4a FDK (AAC)";
        private const string CompressionSettings = @"-cli_cmd=""-m 5 -p 2 --ignorelength -S -o {qt}[outfile]{qt} - """;
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