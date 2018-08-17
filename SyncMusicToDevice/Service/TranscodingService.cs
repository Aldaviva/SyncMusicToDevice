using System.Threading.Tasks;

namespace SyncMusicToDevice.Service
{
    public interface TranscodingService
    {
        Task Transcode(string sourceFile, string destinationFile);
    }
}