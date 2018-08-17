using System;
using System.Collections.Generic;
using SyncMusicToDevice.Data;

namespace SyncMusicToDevice.Database
{
    public interface DeviceMusicDatabase : IDisposable
    {
        string DatabaseFilename { get; }

        SynchronizedMusicFile FindFile(string desktopFilePath);

        SynchronizedMusicFile Save(SynchronizedMusicFile musicFile);

        void Open(string filename);

        void Close();

        IEnumerable<SynchronizedMusicFile> FindAll();

        void DeleteFile(string desktopFilePath);
    }
}