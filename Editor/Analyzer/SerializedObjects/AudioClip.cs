using usingUnity.ProjectAuditor.Editor.UnityFileSystemApi.TypeTreeReaders;

namespace Unity.ProjectAuditor.Editor.Analyzer.SerializedObjects
{
    public class AudioClip
    {
        private string m_Name;
        public string Name => m_Name;
        private int m_StreamDataSize;
        public int StreamDataSize => m_StreamDataSize;
        private int m_BitsPerSample;
        public int BitsPerSample => m_BitsPerSample;
        private int m_Frequency;
        public int Frequency => m_Frequency;
        private int m_Channels;
        public int Channels => m_Channels;
        private int m_LoadType;
        public int LoadType => m_LoadType;
        private int m_Format;
        public int Format => m_Format;

        public AudioClip(RandomAccessReader reader)
        {
            m_Name = reader["m_Name"].GetValue<string>();
            m_Channels = reader["m_Channels"].GetValue<int>();
            m_Format = reader["m_CompressionFormat"].GetValue<int>();
            m_Frequency = reader["m_Frequency"].GetValue<int>();
            m_LoadType = reader["m_LoadType"].GetValue<int>();
            m_BitsPerSample = reader["m_BitsPerSample"].GetValue<int>();
            m_StreamDataSize = reader["m_Resource"]["m_Size"].GetValue<int>();
        }
    }
}
