using Unity.ProjectAuditor.Editor.UnityFileSystemApi.TypeTreeReaders;

namespace Unity.ProjectAuditor.Editor.BuildData.SerializedObjects
{
    public class PPtr
    {
        public int FileId { get; }
        public long PathId { get; }

        public PPtr(RandomAccessReader reader)
        {
            FileId = reader["m_FileID"].GetValue<int>();
            PathId = reader["m_PathID"].GetValue<long>();
        }
    }
}
