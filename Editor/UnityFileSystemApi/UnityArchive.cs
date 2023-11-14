using System;
using System.Collections.Generic;
using System.Text;

namespace usingUnity.ProjectAuditor.Editor.UnityFileSystemApi
{
    // An archive node is a file in an archive.
    public struct ArchiveNode
    {
        public string Path;
        public long Size;
        public ArchiveNodeFlags Flags;
    }

    // Class used to open a Unity archive file (such as an AssetBundle).
    public class UnityArchive : IDisposable
    {
        internal UnityArchiveHandle m_Handle;
        Lazy<List<ArchiveNode>> m_Nodes;

        public IReadOnlyList<ArchiveNode> Nodes => m_Nodes.Value.AsReadOnly();

        internal UnityArchive()
        {
            m_Nodes = new Lazy<List<ArchiveNode>>(() => GetArchiveNodes());
        }

        List<ArchiveNode> GetArchiveNodes()
        {
            var r = DllWrapper.GetArchiveNodeCount(m_Handle, out var count);
            UnityFileSystem.HandleErrors(r);

            if (count == 0)
                return null;

            var nodes = new List<ArchiveNode>(count);
            var path = new StringBuilder(512);

            for (var i = 0; i < count; ++i)
            {
                DllWrapper.GetArchiveNode(m_Handle, i, path, path.Capacity, out var size, out var flags);
                UnityFileSystem.HandleErrors(r);

                nodes.Add(new ArchiveNode() { Path = path.ToString(), Size = size, Flags = flags });
            }

            return nodes;
        }

        public void Dispose()
        {
            if (m_Handle != null && !m_Handle.IsInvalid)
            {
                m_Handle.Dispose();
                m_Nodes = new Lazy<List<ArchiveNode>>(() => GetArchiveNodes());
            }
        }
    }
}
