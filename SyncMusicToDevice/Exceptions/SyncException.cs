using System;

namespace SyncMusicToDevice.Exceptions
{
    public class SyncException : Exception
    {
        protected SyncException(string message) : base(message)
        {
        }

        protected SyncException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    public class DeviceException : SyncException
    {
        public DeviceException(string message) : base(message)
        {
        }

        public DeviceException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    public class MediaException : SyncException
    {
        public MediaException(string message) : base(message)
        {
        }

        public MediaException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    public class DatabaseException : SyncException
    {
        public DatabaseException(string message) : base(message)
        {
        }

        public DatabaseException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}