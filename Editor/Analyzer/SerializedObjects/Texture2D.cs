using usingUnity.ProjectAuditor.Editor.UnityFileSystemApi.TypeTreeReaders;

namespace Unity.ProjectAuditor.Editor.Analyzer.SerializedObjects
{
    public class Texture2D
    {
        private string m_Name;
        public string Name => m_Name;
        private int m_StreamDataSize;
        public int StreamDataSize => m_StreamDataSize;
        private int m_Width;
        public int Width => m_Width;
        private int m_Height;
        public int Height => m_Height;
        private int m_Format;
        public int Format => m_Format;
        private int m_MipCount;
        public int MipCount => m_MipCount;
        private bool m_RwEnabled;
        public bool RwEnabled => m_RwEnabled;

        public Texture2D(RandomAccessReader reader)
        {
            m_Name = reader["m_Name"].GetValue<string>();
            m_Width = reader["m_Width"].GetValue<int>();
            m_Height = reader["m_Height"].GetValue<int>();
            m_Format = reader["m_TextureFormat"].GetValue<int>();
            m_RwEnabled = reader["m_IsReadable"].GetValue<int>() != 0;
            m_MipCount = reader["m_MipCount"].GetValue<int>();
            m_StreamDataSize = reader["image data"].GetArraySize() == 0 ? reader["m_StreamData"]["size"].GetValue<int>() : 0;
        }
    }
}
