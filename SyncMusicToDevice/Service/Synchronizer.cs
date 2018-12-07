using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using NLog;
using SyncMusicToDevice.Data;
using SyncMusicToDevice.Database;
using SyncMusicToDevice.Injection;
using SyncMusicToDevice.Native;

namespace SyncMusicToDevice.Service
{
    public interface Synchronizer
    {
        Task Synchronize();
        string DesktopMusicDirectory { get; set; }
        string DeviceMusicDirectory { get; set; }
    }

    [Component]
    public class SynchronizerImpl : Synchronizer
    {
        private static readonly Logger LOGGER = LogManager.GetCurrentClassLogger();

        private const string DatabaseFilename = "synchronized.sqlite";
        private const int MinBitRateToTranscode = 288;
        private const string TranscodedFileExtension = "m4a";

        private readonly TranscodingService transcodingService;
        private readonly DesktopMusicService desktopMusicService;
        private readonly DeviceMusicService deviceMusicService;
        private readonly DeviceMusicDatabase deviceMusicDatabase;
        private string deviceMusicDirectory;
        private string TemporaryDirectory { get; } = Path.Combine(Path.GetTempPath(), "SyncMusicToDevice");

        public string DesktopMusicDirectory { get; set; }

        public string DeviceMusicDirectory
        {
            get => deviceMusicDirectory;
            set
            {
                deviceMusicDirectory = value;
                deviceMusicService.MusicDirectory = value; //TODO replace with a settings module
            }
        }

        public SynchronizerImpl(TranscodingService transcodingService, DesktopMusicService desktopMusicService,
            DeviceMusicService deviceMusicService, DeviceMusicDatabase deviceMusicDatabase)
        {
            this.transcodingService = transcodingService;
            this.desktopMusicService = desktopMusicService;
            this.deviceMusicService = deviceMusicService;
            this.deviceMusicDatabase = deviceMusicDatabase;
        }

        public async Task Synchronize()
        {
            IEnumerable<string> desktopFiles = ScanDesktopFiles();

            await OpenDatabase();

            IList<SyncOperation> pendingOperations = (await DetermineFilesToSync(desktopFiles))
                .OrderBy(operation => operation.FilePath)
                .ToList();

            bool shouldContinue = PromptForConfirmation(pendingOperations);

            if (shouldContinue)
            {
                await RunOperations(pendingOperations);
            }

            await CloseDatabaseAndCopyToDevice();
        }

        private Task CloseDatabaseAndCopyToDevice()
        {
            string desktopDatabaseFilePath = Path.Combine(TemporaryDirectory, DatabaseFilename);
            deviceMusicDatabase.Close();
            return deviceMusicService.CopyDesktopFileToDevice(desktopDatabaseFilePath, DatabaseFilename);
        }

        private static bool PromptForConfirmation(IList<SyncOperation> pendingOperations)
        {
            int filesToCopyCount = pendingOperations.Count(operation => operation.GetType() == typeof(CopyOperation));
            int filesToTranscodeCount = pendingOperations.Count(operation =>
                operation.GetType() == typeof(CopyOperation) && ((CopyOperation) operation).RequiresTranscoding);
            int filesToDeleteCount = pendingOperations.Count(operation => operation.GetType() == typeof(DeleteOperation));

            Console.WriteLine(
                $"\n{filesToCopyCount:N0} files will be copied ({filesToTranscodeCount:N0} of which will be transcoded), and {filesToDeleteCount:N0} will be deleted.");

            while (true)
            {
                Console.Write("Press Y to continue, N to stop, or P to preview changes: ");
                ConsoleKeyInfo continueResponse = Console.ReadKey();
                Console.WriteLine();
                if (continueResponse.Key == ConsoleKey.Escape)
                {
                    return false;
                }

                switch (continueResponse.KeyChar.ToString().ToLowerInvariant())
                {
                    case "n":
                        return false;
                    case "y":
                        return true;
                    case "p":
                        PreviewChanges(pendingOperations);
                        break;
                    // ReSharper disable once RedundantEmptySwitchSection I'm explaining why it's empty!
                    default:
                        // user pressed a different key, show prompt again
                        break;
                }
            }
        }

        private static void PreviewChanges(IEnumerable<SyncOperation> pendingOperations)
        {
            foreach (SyncOperation pendingOperation in pendingOperations)
            {
                string changeLabel = "";
                switch (pendingOperation)
                {
                    case CopyOperation copy:
                        changeLabel = $"{(copy.RequiresTranscoding ? "transcode" : "copy     ")} {pendingOperation.FilePath}";
                        break;
                    case DeleteOperation _:
                        changeLabel = $"delete    {pendingOperation.FilePath}";
                        break;
                }

                Console.WriteLine(changeLabel);
            }
        }

        private async Task RunOperations(IEnumerable<SyncOperation> pendingOperations)
        {
            var actionBlock = new ActionBlock<SyncOperation>(operation =>
            {
                switch (operation)
                {
                    case CopyOperation copyOperation:
                        return RunOperation(copyOperation);
                    case DeleteOperation deleteOperation:
                        return RunOperation(deleteOperation);
                    default:
                        throw new Exception($"Unknown operation type {operation.GetType()}");
                }
            }, new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount
            });

            foreach (SyncOperation pendingOperation in pendingOperations)
            {
                actionBlock.Post(pendingOperation);
            }

            actionBlock.Complete();
            await actionBlock.Completion;
        }

        private async Task RunOperation(CopyOperation operation)
        {
            string fileToCopy;
            string destinationFilePath =
                PathRelativePathTo.GetRelativePath(new DirectoryInfo(DesktopMusicDirectory), new FileInfo(operation.FilePath));

            if (operation.RequiresTranscoding)
            {
                // dbPoweramp will only write ID tags if it recognizes the destination file extension, so .tmp leads to untagged files
                fileToCopy = Path.ChangeExtension(Path.GetTempFileName(), TranscodedFileExtension);
                destinationFilePath = Path.ChangeExtension(destinationFilePath, TranscodedFileExtension);
                await transcodingService.Transcode(operation.FilePath, fileToCopy);
            }
            else
            {
                fileToCopy = operation.FilePath;
            }

            await deviceMusicService.CopyDesktopFileToDevice(fileToCopy, destinationFilePath);

            deviceMusicDatabase.Save(new SynchronizedMusicFile
            {
                DesktopFilePath = operation.FilePath,
                DeviceFileName = Path.GetFileName(destinationFilePath),
                Checksum = await desktopMusicService.GetChecksum(operation.FilePath),
                Modified = desktopMusicService.GetDateModified(operation.FilePath)
            });

            if (operation.RequiresTranscoding)
            {
                LOGGER.Trace($"Cleaning up temporary transcoded file {fileToCopy}");
                File.Delete(fileToCopy);
            }

            LOGGER.Info("{0} to device: {1}", operation.RequiresTranscoding ? "Transcoded" : "    Copied", operation.FilePath);
        }

        private async Task RunOperation(DeleteOperation operation)
        {
            LOGGER.Debug("Deleting {0} from device...", operation.FilePath);
            await deviceMusicService.DeleteFileFromDevice(operation.FilePath);
            deviceMusicDatabase.DeleteFile(operation.DesktopFile.DesktopFilePath);
            LOGGER.Info("Deleted from device: {0}", operation.FilePath);
        }

        private async Task<IEnumerable<SyncOperation>> DetermineFilesToSync(IEnumerable<string> desktopFiles)
        {
            LOGGER.Debug("Determining which files to synchronize...");
            var pendingCopies = new ConcurrentBag<SyncOperation>();
            var deviceFilesToNotDelete = new ConcurrentBag<SynchronizedMusicFile>();

            var actionBlock = new ActionBlock<string>(async desktopFile =>
            {
                SynchronizedMusicFile fileOnDevice = deviceMusicDatabase.FindFile(desktopFile);
                CopyOperation pendingOperation = null;

                if (fileOnDevice == null)
                {
                    pendingOperation = new CopyOperation(desktopFile, await DoesFileRequireTranscoding(desktopFile));
                }
                else // file already exists on device according to sync database
                {
                    deviceFilesToNotDelete.Add(fileOnDevice);

                    DateTime dateModified = desktopMusicService.GetDateModified(desktopFile);
                    if (dateModified == fileOnDevice.Modified)
                    {
                        //same date, skipping file
                    }
                    else if ((await desktopMusicService.GetChecksum(desktopFile)).SequenceEqual(fileOnDevice.Checksum))
                    {
                        //different date, but same checksum, skipping file
                    }
                    else
                    {
                        //different date, different checksum, copying file
                        pendingOperation = new CopyOperation(desktopFile, await DoesFileRequireTranscoding(desktopFile));
                    }
                }

                if (pendingOperation != null)
                {
                    pendingCopies.Add(pendingOperation);
                }

                Console.Write('.');
            }, new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount
            });

            foreach (string desktopFile in desktopFiles)
            {
                actionBlock.Post(desktopFile);
            }

            actionBlock.Complete();
            await actionBlock.Completion;

            IEnumerable<DeleteOperation> pendingDeletions = deviceMusicDatabase.FindAll()
                .Except(deviceFilesToNotDelete, new DeletionComparer())
                .Select(file =>
                {
                    string deviceFilePath = PathRelativePathTo.GetRelativePath(new DirectoryInfo(DesktopMusicDirectory), new FileInfo(file.DesktopFilePath));
                    return new DeleteOperation(file, deviceFilePath);
                });

            return pendingCopies.Concat(pendingDeletions);
        }

        private async Task<bool> DoesFileRequireTranscoding(string desktopFile)
        {
            return await desktopMusicService.GetBitrate(desktopFile) >= MinBitRateToTranscode;
        }

        private IEnumerable<string> ScanDesktopFiles()
        {
            LOGGER.Info("Scanning music files on the desktop...");
            return desktopMusicService.ListFiles(DesktopMusicDirectory);
        }

        private async Task OpenDatabase()
        {
            Directory.CreateDirectory(TemporaryDirectory);
            string desktopDatabaseFilePath = Path.Combine(TemporaryDirectory, DatabaseFilename);
            if (File.Exists(desktopDatabaseFilePath))
            {
                string backupDatabaseFilePath = Path.ChangeExtension(desktopDatabaseFilePath, "bak");
                File.Delete(backupDatabaseFilePath);
                File.Move(desktopDatabaseFilePath, backupDatabaseFilePath);
            }

            if (await deviceMusicService.DoesFileExistOnDevice(DatabaseFilename))
            {
                LOGGER.Info($"Downloading synchronized music database from device to {desktopDatabaseFilePath}");
                await deviceMusicService.CopyDeviceFileToDesktop(DatabaseFilename,
                    desktopDatabaseFilePath);
                LOGGER.Info("Database downloaded.");
            }
            else
            {
                LOGGER.Warn(
                    $"Could not find existing synchronized music database file ({DeviceMusicDirectory}/{DatabaseFilename}) on device. ");
                LOGGER.Warn("Creating new empty database file and assuming that no files have been synchronized. ");
                LOGGER.Warn(
                    $"If this is not the case, please find {DatabaseFilename} and put it in {DeviceMusicDirectory} on your device.");
            }

            deviceMusicDatabase.Open(desktopDatabaseFilePath);
        }
    }

    internal class DeletionComparer : IEqualityComparer<SynchronizedMusicFile>
    {
        public bool Equals(SynchronizedMusicFile x, SynchronizedMusicFile y)
        {
            if (x == null && y == null)
            {
                return true;
            }
            else if (x == null || y == null)
            {
                return false;
            }
            else
            {
                return x.DesktopFilePath == y.DesktopFilePath;
            }
        }

        public int GetHashCode(SynchronizedMusicFile obj)
        {
            return obj.DesktopFilePath.GetHashCode();
        }
    }
}