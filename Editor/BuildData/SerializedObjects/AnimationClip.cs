using Unity.ProjectAuditor.Editor.UnityFileSystemApi;

namespace Unity.ProjectAuditor.Editor.BuildData.SerializedObjects
{
    public class AnimationClip : SerializedObject
    {
        public bool Legacy { get; }
        public int Events { get; }

        public AnimationClip(BuildFileInfo buildFile, PPtrResolver pPtrResolver, TypeTreeReader reader, int id, long size, uint crc32)
            : base(buildFile, pPtrResolver, reader, id, size, crc32,"AnimationClip")
        {
            Legacy = reader["m_Legacy"].GetValue<bool>();
            Events = reader["m_Events"].GetArraySize();
        }
    }
}
