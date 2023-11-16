using Unity.ProjectAuditor.Editor.UnityFileSystemApi.TypeTreeReaders;

namespace Unity.ProjectAuditor.Editor.BuildData.SerializedObjects
{
    public class Texture2D : SerializedObject
    {
        public int Width { get; }
        public int Height { get; }
        public int Format { get; }
        public int MipCount { get; }
        public bool RwEnabled { get; }

        public Texture2D(RandomAccessReader reader, long size, BuildFileInfo buildFile)
            : base(reader, size, "Texture2D", buildFile)
        {
            Width = reader["m_Width"].GetValue<int>();
            Height = reader["m_Height"].GetValue<int>();
            Format = reader["m_TextureFormat"].GetValue<int>();
            RwEnabled = reader["m_IsReadable"].GetValue<int>() != 0;
            MipCount = reader["m_MipCount"].GetValue<int>();

            Size += reader["image data"].GetArraySize() == 0 ? reader["m_StreamData"]["size"].GetValue<int>() : 0;
        }
    }
}
