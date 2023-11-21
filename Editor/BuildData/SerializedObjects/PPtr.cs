using Unity.ProjectAuditor.Editor.UnityFileSystemApi;

namespace Unity.ProjectAuditor.Editor.BuildData.SerializedObjects
{
    public class PPtr
    {
        public int ObjectId { get; }

        public PPtr(PPtrResolver pPtrResolver, TypeTreeReader reader)
        {
            var fileId = reader["m_FileID"].GetValue<int>();
            var pathId = reader["m_PathID"].GetValue<long>();

            ObjectId = pPtrResolver.GetObjectId(fileId, pathId);
        }
    }
}
