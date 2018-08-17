namespace SyncMusicToDevice.Data
{
    public interface SyncOperation
    {
        string FilePath { get; }
    }

    public class CopyOperation : SyncOperation
    {
        public string FilePath { get; }
        public bool RequiresTranscoding { get; }

        public CopyOperation(string desktopFilePath, bool requiresTranscoding)
        {
            FilePath = desktopFilePath;
            RequiresTranscoding = requiresTranscoding;
        }
    }

    public class DeleteOperation : SyncOperation
    {
        public SynchronizedMusicFile DesktopFile { get; }
        public string FilePath { get; }

        public DeleteOperation(SynchronizedMusicFile desktopFile, string deviceFilePath)
        {
            FilePath = deviceFilePath;
            DesktopFile = desktopFile;
        }
    }
    
}