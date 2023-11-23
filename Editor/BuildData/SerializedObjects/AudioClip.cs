using Unity.ProjectAuditor.Editor.UnityFileSystemApi;

namespace Unity.ProjectAuditor.Editor.BuildData.SerializedObjects
{
    public class AudioClip : SerializedObject
    {
        public enum CompressionFormat
        {
            PCM = 0,
            Vorbis = 1,
            ADPCM = 2,
            MP3 = 3,
            PSMVAG = 4,
            HEVAG = 5,
            XMA = 6,
            AAC = 7,
            GCADPCM = 8,
            ATRAC9 = 9
        };

        public enum AudioLoadType
        {
            DecompressOnLoad = 0,
            CompressedInMemory = 1,
            Streaming = 2,
        };

        public int BitsPerSample { get; }
        public int Frequency { get; }
        public int Channels { get; }
        public AudioLoadType LoadType { get; }
        public CompressionFormat Format { get; }

        public AudioClip(ObjectInfo obj, BuildFileInfo buildFile, PPtrResolver pPtrResolver, TypeTreeReader reader, uint crc32)
            : base(obj, buildFile, pPtrResolver, reader, crc32)
        {
            Channels = reader["m_Channels"].GetValue<int>();
            Format = (CompressionFormat)reader["m_CompressionFormat"].GetValue<int>();
            Frequency = reader["m_Frequency"].GetValue<int>();
            LoadType = (AudioLoadType)reader["m_LoadType"].GetValue<int>();
            BitsPerSample = reader["m_BitsPerSample"].GetValue<int>();

            Size += reader["m_Resource"]["m_Size"].GetValue<int>();
        }
    }
}
