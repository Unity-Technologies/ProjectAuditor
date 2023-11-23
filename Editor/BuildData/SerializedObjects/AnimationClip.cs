using Unity.ProjectAuditor.Editor.UnityFileSystemApi;

namespace Unity.ProjectAuditor.Editor.BuildData.SerializedObjects
{
    public class AnimationClip : SerializedObject
    {
        public bool Legacy { get; }
        public int Events { get; }

        public AnimationClip(ObjectInfo obj, BuildFileInfo buildFile, PPtrResolver pPtrResolver, TypeTreeReader reader, uint crc32)
            : base(obj, buildFile, pPtrResolver, reader, crc32)
        {
            Legacy = reader["m_Legacy"].GetValue<bool>();
            Events = reader["m_Events"].GetArraySize();
        }
    }
}
