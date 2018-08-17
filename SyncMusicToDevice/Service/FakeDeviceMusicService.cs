using System.Threading.Tasks;
using NLog;
using SyncMusicToDevice.Injection;

namespace SyncMusicToDevice.Service
{
    //[Component] //FIXME
    public class FakeDeviceMusicService : DeviceMusicService
    {
        private static readonly Logger LOGGER = LogManager.GetCurrentClassLogger();

        public string MusicDirectory { get; set; } = @"\Music2";

        public Task CopyDesktopFileToDevice(string sourceDesktopFilePath, string destinationDeviceFilePath)
        {
            LOGGER.Debug($"Copying file {sourceDesktopFilePath} from desktop to {destinationDeviceFilePath} on fake device...");
            return Task.Delay(200);
        }

        public Task CopyDeviceFileToDesktop(string sourceDeviceFilePath, string destinationDesktopFilePath)
        {
            LOGGER.Debug($"Copying file {sourceDeviceFilePath} from fake device to {destinationDesktopFilePath} on desktop...");
            return Task.Delay(200);
        }

        public Task<bool> DoesFileExistOnDevice(string deviceFilePath)
        {
            LOGGER.Debug($"Checking if file {deviceFilePath} exists on the device...");
            return Task.Delay(200).ContinueWith(task => false);
        }

        public Task DeleteFileFromDevice(string deviceFilePath)
        {
            LOGGER.Debug($"Deleting file {deviceFilePath} from device...");
            return Task.Delay(200);
        }
    }
}