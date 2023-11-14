using usingUnity.ProjectAuditor.Editor.UnityFileSystemApi.TypeTreeReaders;

namespace Unity.ProjectAuditor.Editor.Analyzer.SerializedObjects
{
    public class PPtr
    {
        private int m_FileId;
        public int FileId => m_FileId;
        private long m_PathId;
        public long PathId => m_PathId;

        public PPtr(RandomAccessReader reader)
        {
            m_FileId = reader["m_FileID"].GetValue<int>();
            m_PathId = reader["m_PathID"].GetValue<long>();
        }
    }
}
