using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaDevices;
using NLog;
using SyncMusicToDevice.Exceptions;
using SyncMusicToDevice.Injection;

namespace SyncMusicToDevice.Service
{
    [Component]
    public class MtpDeviceMusicService : DeviceMusicService, IDisposable
    {
        private static readonly Logger LOGGER = LogManager.GetCurrentClassLogger();

        private readonly MediaDevice device;

        public string MusicDirectory { get; set; } = @"\Music"; //TODO replace with a settings module
        private readonly string rootDirectory;

        public MtpDeviceMusicService()
        {
            List<MediaDevice> mediaDevices = MediaDevice.GetDevices().ToList();
            switch (mediaDevices.Count)
            {
                case 0:
                    throw new DeviceException(
                        "No attached MTP media devices found, make sure your Android phone's USB mode is set to MTP.");
                case 1:
                    device = mediaDevices[0];
                    break;
                case int _ when mediaDevices.Count > 1:
                    throw new DeviceException("Too many devices, not sure which one to use.");
            }

            device.Connect();

            // this throws exceptions if the program is built for x64
            // https://github.com/Bassman2/MediaDevices/issues/6
            rootDirectory = device.GetRootDirectory().EnumerateDirectories().First().Name;
        }

        public void Dispose()
        {
            device?.Dispose();
        }

        public Task CopyDesktopFileToDevice(string sourceDesktopFilePath, string destinationDeviceFilePath)
        {
            return Task.Run(() =>
            {
                Monitor.Enter(this);
                try
                {
                    LOGGER.Debug("Copying {0} to device...", sourceDesktopFilePath);
                    destinationDeviceFilePath = GetAbsoluteDevicePath(destinationDeviceFilePath);
                    device.CreateDirectory(Path.GetDirectoryName(destinationDeviceFilePath));

                    if (device.FileExists(destinationDeviceFilePath))
                    {
                        device.DeleteFile(destinationDeviceFilePath);
                    }

                    device.UploadFile(sourceDesktopFilePath, destinationDeviceFilePath);
                    LOGGER.Trace("Finished copying {0}", sourceDesktopFilePath);
                }
                finally
                {
                    Monitor.Exit(this);
                }
            });
        }

        public Task CopyDeviceFileToDesktop(string sourceDeviceFilePath, string destinationDesktopFilePath)
        {
            return Task.Run(() =>
            {
                Monitor.Enter(this);
                try
                {
                    sourceDeviceFilePath = GetAbsoluteDevicePath(sourceDeviceFilePath);
                    string directory = Path.GetDirectoryName(destinationDesktopFilePath);
                    if (directory != null)
                    {
                        Directory.CreateDirectory(directory);
                    }

                    device.DownloadFile(sourceDeviceFilePath, destinationDesktopFilePath);
                }
                finally
                {
                    Monitor.Exit(this);
                }
            });
        }

        public Task<bool> DoesFileExistOnDevice(string deviceFilePath)
        {
            return Task.Run(() =>
            {
                Monitor.Enter(this);
                try
                {
                    return device.FileExists(GetAbsoluteDevicePath(deviceFilePath));
                }
                finally
                {
                    Monitor.Exit(this);
                }
            });
        }

        public Task DeleteFileFromDevice(string deviceFilePath)
        {
            return Task.Run(() =>
            {
                Monitor.Enter(this);
                try
                {
                    device.DeleteFile(GetAbsoluteDevicePath(deviceFilePath));
                }
                finally
                {
                    Monitor.Exit(this);
                }
            });
        }

        private string GetAbsoluteDevicePath(string devicePathRelativeToMusicDirectory)
        {
            return Path.Combine(rootDirectory, MusicDirectory, devicePathRelativeToMusicDirectory);
        }
    }
}