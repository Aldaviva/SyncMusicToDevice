using System.Collections.Generic;
using SQLite;
using SyncMusicToDevice.Data;
using SyncMusicToDevice.Exceptions;
using SyncMusicToDevice.Injection;

namespace SyncMusicToDevice.Database
{
    [Component]
    public class SqliteDeviceMusicDatabase : DeviceMusicDatabase
    {
        private SQLiteConnection connection;

        public string DatabaseFilename { get; private set; }
        
        public void Open(string filename)
        {
            DatabaseFilename = filename;

            connection = new SQLiteConnection(DatabaseFilename);
            connection.CreateTable<SynchronizedMusicFile>();
        }

        public void Close()
        {
            connection?.Close();
            connection = null;
        }

        public void Dispose()
        {
            connection?.Dispose();
            connection = null;
        }

        /// <summary>
        /// Get a <see cref="SynchronizedMusicFile"/> by <see cref="SynchronizedMusicFile.DesktopFilePath"/>.
        /// </summary>
        /// <param name="desktopFilePath">Value to exactly match on the <see cref="SynchronizedMusicFile.DesktopFilePath"/> of the database entries.</param>
        /// <returns>Returns a <see cref="SynchronizedMusicFile"/> from the database that has the same <see cref="SynchronizedMusicFile.DesktopFilePath"/>, or <c>null</c> if no such file exists in the database.</returns>
        /// <exception>Throws a "not found exception" if no entry matching the given <c>desktopFilePath</c> is found.</exception>
        public SynchronizedMusicFile FindFile(string desktopFilePath)
        {
            EnsureOpen();
            try
            {
                return connection.Get<SynchronizedMusicFile>(desktopFilePath);
            }
            catch
            {
                return null;
            }
        }

        public IEnumerable<SynchronizedMusicFile> FindAll()
        {
            EnsureOpen();
            return connection.Table<SynchronizedMusicFile>();
        }

        public void DeleteFile(string desktopFilePath)
        {
            EnsureOpen();
            connection.Delete<SynchronizedMusicFile>(desktopFilePath);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="musicFile"></param>
        /// <returns></returns>
        public SynchronizedMusicFile Save(SynchronizedMusicFile musicFile)
        {
            EnsureOpen();
            connection.InsertOrReplace(musicFile);
            return musicFile;
        }

        private void EnsureOpen()
        {
            if (connection == null)
            {
                throw new DatabaseException("Connection closed, call .open() first.");
            }
        }
    }
}