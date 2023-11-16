using Unity.ProjectAuditor.Editor.UnityFileSystemApi.TypeTreeReaders;

namespace Unity.ProjectAuditor.Editor.BuildData.SerializedObjects
{
    public class AnimationClip : SerializedObject
    {
        public bool Legacy { get; }
        public int Events { get; }

        public AnimationClip(RandomAccessReader reader, long size, BuildFileInfo buildFile)
            : base(reader, size, "AnimationClip", buildFile)
        {
            Legacy = reader["m_Legacy"].GetValue<bool>();
            Events = reader["m_Events"].GetArraySize();
        }
    }
}
