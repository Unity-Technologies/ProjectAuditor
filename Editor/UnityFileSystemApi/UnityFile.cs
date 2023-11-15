using System;

namespace usingUnity.ProjectAuditor.Editor.UnityFileSystemApi
{
    // Use this class to read data from a Unity file.
    public class UnityFile : IDisposable
    {
        internal UnityFile()
        {
        }

        internal UnityFileHandle m_Handle;

        public long Read(long size, byte[] buffer)
        {
            var r = DllWrapper.ReadFile(m_Handle, size, buffer, out var actualSize);
            UnityFileSystem.HandleErrors(r);

            return actualSize;
        }

        public long Seek(long offset, SeekOrigin origin = SeekOrigin.Begin)
        {
            var r = DllWrapper.SeekFile(m_Handle, offset, origin, out var newPosition);
            UnityFileSystem.HandleErrors(r);

            return newPosition;
        }

        public long GetSize()
        {
            // This could be a property but as it may throw an exception, it's probably better as a method.

            var r = DllWrapper.GetFileSize(m_Handle, out var size);
            UnityFileSystem.HandleErrors(r);

            return size;
        }

        public void Dispose()
        {
            if (m_Handle != null && !m_Handle.IsInvalid)
            {
                m_Handle.Dispose();
            }
        }
    }
}
