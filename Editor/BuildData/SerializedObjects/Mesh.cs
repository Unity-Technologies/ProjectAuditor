using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.UnityFileSystemApi;

namespace Unity.ProjectAuditor.Editor.BuildData.SerializedObjects
{
    public class Mesh : SerializedObject
    {
        public enum MeshCompression
        {
            Off = 0,
            Low = 1,
            Med = 2,
            High = 3,
        };

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

        public int SubMeshes { get; }
        public int BlendShapes { get; }
        public int Bones { get; }
        public int Indices { get; }
        public int Vertices { get; }
        public MeshCompression Compression { get; }
        public bool RwEnabled { get; }
        public IReadOnlyList<Channel> Channels { get; }
        public int VertexSize { get; }

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

        public Mesh(BuildFileInfo buildFile, PPtrResolver pPtrResolver, TypeTreeReader reader, int id, long size, uint crc32)
            : base(buildFile, pPtrResolver, reader, id, size, crc32, "Mesh")
        {
            Compression = (MeshCompression)reader["m_MeshCompression"].GetValue<byte>();
            var channels = new List<Channel>();

            if (Compression == MeshCompression.Off)
            {
                var bytesPerIndex = reader["m_IndexFormat"].GetValue<int>() == 0 ? 2 : 4;

                Indices = reader["m_IndexBuffer"].GetArraySize() / bytesPerIndex;
                Vertices = reader["m_VertexData"]["m_VertexCount"].GetValue<int>();

                // If vertex data size is 0, data is stored in a stream file.
                if (reader["m_VertexData"]["m_DataSize"].GetArraySize() == 0)
                {
                    Size += reader["m_StreamData"]["size"].GetValue<int>();
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

                        channels.Add(c);
                        VertexSize += dimension * s_ChannelTypeSizes[(int)c.Type];
                    }

                    ++i;
                }
            }
            else
            {
                Vertices = reader["m_CompressedMesh"]["m_Vertices"]["m_NumItems"].GetValue<int>() / 3;
                Indices = reader["m_CompressedMesh"]["m_Triangles"]["m_NumItems"].GetValue<int>();
            }

            SubMeshes = reader["m_SubMeshes"].GetArraySize();
            BlendShapes = reader["m_Shapes"]["shapes"].GetArraySize();
            Bones = reader["m_BoneNameHashes"].GetArraySize();
            RwEnabled = reader["m_IsReadable"].GetValue<int>() != 0;
            Channels = channels;
        }
    }
}
