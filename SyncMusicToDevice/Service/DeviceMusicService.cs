using System.Threading.Tasks;

namespace SyncMusicToDevice.Service
{
    public interface DeviceMusicService
    {
        string MusicDirectory { get; set; }

        Task CopyDesktopFileToDevice(string sourceDesktopFilePath, string destinationDeviceFilePath);

        Task CopyDeviceFileToDesktop(string sourceDeviceFilePath, string destinationDesktopFilePath);

        Task<bool> DoesFileExistOnDevice(string deviceFilePath);

        Task DeleteFileFromDevice(string deviceFilePath);
    }
}