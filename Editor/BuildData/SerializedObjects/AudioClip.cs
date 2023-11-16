using Unity.ProjectAuditor.Editor.UnityFileSystemApi.TypeTreeReaders;

namespace Unity.ProjectAuditor.Editor.BuildData.SerializedObjects
{
    public class AudioClip : SerializedObject
    {
        public int BitsPerSample { get; }
        public int Frequency { get; }
        public int Channels { get; }
        public int LoadType { get; }
        public int Format { get; }

        public AudioClip(RandomAccessReader reader, long size, BuildFileInfo buildFile)
            : base(reader, size, "AudioClip", buildFile)
        {
            Channels = reader["m_Channels"].GetValue<int>();
            Format = reader["m_CompressionFormat"].GetValue<int>();
            Frequency = reader["m_Frequency"].GetValue<int>();
            LoadType = reader["m_LoadType"].GetValue<int>();
            BitsPerSample = reader["m_BitsPerSample"].GetValue<int>();

            Size += reader["m_Resource"]["m_Size"].GetValue<int>();
        }
    }
}
