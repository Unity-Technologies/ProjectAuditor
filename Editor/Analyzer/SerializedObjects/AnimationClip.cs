using usingUnity.ProjectAuditor.Editor.UnityFileSystemApi.TypeTreeReaders;

namespace Unity.ProjectAuditor.Editor.Analyzer.SerializedObjects
{
    public class AnimationClip
    {
        private string m_Name;
        public string Name => m_Name;
        private bool m_Legacy;
        public bool Legacy => m_Legacy;
        private int m_Events;
        public int Events => m_Events;

        public AnimationClip(RandomAccessReader reader)
        {
            m_Name = reader["m_Name"].GetValue<string>();
            m_Legacy = reader["m_Legacy"].GetValue<bool>();
            m_Events = reader["m_Events"].GetArraySize();
        }
    }
}
