using System;
using System.Collections.Generic;
using usingUnity.ProjectAuditor.Editor.UnityFileSystemApi.TypeTreeReaders;

namespace Unity.ProjectAuditor.Editor.Analyzer.SerializedObjects
{
    public class Mesh
    {
        public enum ChannelUsage
        {
            Vertex,
            Normal,
            Tangent,
            Color,
            TexCoord0,
            TexCoord1,
            TexCoord2,
            TexCoord3,
            TexCoord4,
            TexCoord5,
            TexCoord6,
            TexCoord7,
            BlendWeights,
            BlendIndices,
        };

        public enum ChannelType
        {
            Float,
            Float16,
            UNorm8,
            SNorm8,
            UNorm16,
            SNorm16,
            UInt8,
            SInt8,
            UInt16,
            SInt16,
            UInt32,
            SInt32,
        };

        public class Channel
        {
            public ChannelUsage Usage;
            public ChannelType Type;
            public int Dimension;
        }

        private string m_Name;
        public string Name => m_Name;
        private int m_StreamDataSize;
        public int StreamDataSize => m_StreamDataSize;
        private int m_SubMeshes;
        public int SubMeshes => m_SubMeshes;
        private int m_BlendShapes;
        public int BlendShapes => m_BlendShapes;
        private int m_Bones;
        public int Bones => m_Bones;
        private int m_Indices;
        public int Indices => m_Indices;
        private int m_Vertices;
        public int Vertices => m_Vertices;
        private int m_Compression;
        public int Compression => m_Compression;
        private bool m_RwEnabled;
        public bool RwEnabled => m_RwEnabled;

        private List<Channel> m_Channels;
        public IReadOnlyList<Channel> Channels => m_Channels;

        private int m_VertexSize;
        public int VertexSize => m_VertexSize;

        private static readonly int[] s_ChannelTypeSizes =
        {
            4,  // Float
            2,  // Float16
            1,  // UNorm8
            1,  // SNorm8
            2,  // UNorm16
            2,  // SNorm16
            1,  // UInt8
            1,  // SInt8
            2,  // UInt16
            2,  // SInt16
            4,  // UInt32
            4,  // SInt32
        };

        public Mesh(RandomAccessReader reader)
        {
            m_Name = reader["m_Name"].GetValue<string>();
            m_Compression = reader["m_MeshCompression"].GetValue<byte>();
            m_Channels = new List<Channel>();

            if (m_Compression == 0)
            {
                var bytesPerIndex = reader["m_IndexFormat"].GetValue<int>() == 0 ? 2 : 4;

                m_Indices = reader["m_IndexBuffer"].GetArraySize() / bytesPerIndex;
                m_Vertices = reader["m_VertexData"]["m_VertexCount"].GetValue<int>();

                // If vertex data size is 0, data is stored in a stream file.
                if (reader["m_VertexData"]["m_DataSize"].GetArraySize() == 0)
                {
                    m_StreamDataSize = reader["m_StreamData"]["size"].GetValue<int>();
                }

                int i = 0;
                foreach (var channel in reader["m_VertexData"]["m_Channels"])
                {
                    int dimension = channel["dimension"].GetValue<byte>();

                    if (dimension != 0)
                    {
                        // The dimension can be padded. In that case, the real dimension
                        // is encoded in the top nibble.
                        int originalDim = (dimension >> 4) & 0xF;
                        if (originalDim != 0)
                        {
                            dimension = originalDim;
                        }

                        var c = new Channel()
                        {
                            Dimension = dimension,
                            Type = (ChannelType)channel["format"].GetValue<byte>(),
                            Usage = (ChannelUsage)i,
                        };

                        m_Channels.Add(c);
                        m_VertexSize += dimension * s_ChannelTypeSizes[(int)c.Type];
                    }

                    ++i;
                }
            }
            else
            {
                m_Vertices = reader["m_CompressedMesh"]["m_Vertices"]["m_NumItems"].GetValue<int>() / 3;
                m_Indices = reader["m_CompressedMesh"]["m_Triangles"]["m_NumItems"].GetValue<int>();
            }

            m_SubMeshes = reader["m_SubMeshes"].GetArraySize();
            m_BlendShapes = reader["m_Shapes"]["shapes"].GetArraySize();
            m_Bones = reader["m_BoneNameHashes"].GetArraySize();
            m_RwEnabled = reader["m_IsReadable"].GetValue<int>() != 0;
        }
    }
}
