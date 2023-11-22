using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.ProjectAuditor.Editor.UnityFileSystemApi;
using Unity.ProjectAuditor.Editor.Utils;

namespace Unity.ProjectAuditor.Editor.BuildData
{
    // This class is used to extract all the PPtrs in a serialized object. It executes a callback whenever a PPtr is found.
    // It provides a string representing the property path of the property (e.g. "m_MyObject.m_MyArray[2].m_PPtrProperty").
    public class PPtrAndCrcProcessor : IDisposable
    {
        SerializedFile m_SerializedFile;
        UnityFileReader m_Reader;
        PPtrResolver m_PPtrResolver;
        BuildObjects m_BuildObjects;
        long m_Offset;
        int m_ObjectId;
        uint m_Crc32;
        string m_Folder;
        StringBuilder m_StringBuilder = new StringBuilder();
        byte[] m_PPtrBytes = new byte[4];

        Dictionary<string, UnityFileReader> m_resourceReaders = new Dictionary<string, UnityFileReader>();

        public PPtrAndCrcProcessor(SerializedFile serializedFile, UnityFileReader reader, string folder, PPtrResolver pPtrResolver, BuildObjects buildObjects)
        {
            m_SerializedFile = serializedFile;
            m_Reader = reader;
            m_Folder = folder;
            m_PPtrResolver = pPtrResolver;
            m_BuildObjects = buildObjects;
        }

        public void Dispose()
        {
            foreach (var r in m_resourceReaders.Values)
            {
                r.Dispose();
            }

            m_resourceReaders.Clear();
        }

        UnityFileReader GetResourceReader(string filename)
        {
            var slashPos = filename.LastIndexOf('/');
            if (slashPos > 0)
            {
                filename = filename.Remove(0, slashPos + 1);
            }

            if (!m_resourceReaders.TryGetValue(filename, out var reader))
            {
                reader = new UnityFileReader(Path.Combine(m_Folder, filename), 4 * 1024 * 1024);
                m_resourceReaders[filename] = reader;
            }

            return reader;
        }

        public uint Process(int objectId, long offset, TypeTreeNode node)
        {
            m_Offset = offset;
            m_ObjectId = objectId;
            m_Crc32 = 0;

            foreach (var child in node.Children)
            {
                m_StringBuilder.Clear();
                m_StringBuilder.Append(child.Name);
                ProcessNode(child);
            }

            return m_Crc32;
        }

        void ProcessNode(TypeTreeNode node)
        {
            if (node.IsBasicType)
            {
                m_Crc32 = m_Reader.ComputeCRC(m_Offset, node.Size, m_Crc32);
                m_Offset += node.Size;
            }
            else if (node.IsArray)
            {
                ProcessArray(node);
            }
            else if (node.Type == "vector" || node.Type == "map" || node.Type == "staticvector")
            {
                ProcessArray(node.Children[0]);
            }
            else if (node.Type.StartsWith("PPtr<"))
            {
                var startIndex = node.Type.IndexOf('<') + 1;
                var endIndex = node.Type.Length - 1;
                var referencedType = node.Type.Substring(startIndex, endIndex - startIndex);

                ExtractPPtr(referencedType);
            }
            else if (node.Type == "StreamingInfo")
            {
                if (node.Children.Count != 3)
                    throw new Exception("Invalid StreamingInfo");

                var offset = node.Children[0].Size == 4 ? m_Reader.ReadInt32(m_Offset) : m_Reader.ReadInt64(m_Offset);
                m_Offset += node.Children[0].Size;

                var size = m_Reader.ReadInt32(m_Offset);
                m_Offset += 4;

                var stringSize = m_Reader.ReadInt32(m_Offset);
                var filename = m_Reader.ReadString(m_Offset + 4, stringSize);
                m_Offset += stringSize + 4;
                m_Offset = (m_Offset + 3) & ~(3);

                if (size > 0)
                {
                    var resourceFile = GetResourceReader(filename);
                    m_Crc32 = resourceFile.ComputeCRC(offset, size, m_Crc32);
                }
            }
            else if (node.Type == "StreamedResource")
            {
                if (node.Children.Count != 3)
                    throw new Exception("Invalid StreamedResource");

                var stringSize = m_Reader.ReadInt32(m_Offset);
                var filename = m_Reader.ReadString(m_Offset + 4, stringSize);
                m_Offset += stringSize + 4;
                m_Offset = (m_Offset + 3) & ~(3);

                var offset = m_Reader.ReadInt64(m_Offset);
                m_Offset += 8;

                var size = (int)m_Reader.ReadInt64(m_Offset);
                m_Offset += 8;

                if (size > 0)
                {
                    var resourceFile = GetResourceReader(filename);
                    m_Crc32 = resourceFile.ComputeCRC(offset, size, m_Crc32);
                }
            }
            else if (node.CSharpType == typeof(string))
            {
                var prevOffset = m_Offset;
                m_Offset += m_Reader.ReadInt32(m_Offset) + 4;
                m_Crc32 = m_Reader.ComputeCRC(prevOffset, (int)(m_Offset - prevOffset), m_Crc32);
            }
            else if (node.IsManagedReferenceRegistry)
            {
                ProcessManagedReferenceRegistry(node);
            }
            else
            {
                foreach (var child in node.Children)
                {
                    var size = m_StringBuilder.Length;
                    m_StringBuilder.Append('.');
                    m_StringBuilder.Append(child.Name);
                    ProcessNode(child);
                    m_StringBuilder.Remove(size, m_StringBuilder.Length - size);
                }
            }

            if (
                ((int)node.MetaFlags & (int)TypeTreeMetaFlags.AlignBytes) != 0 ||
                ((int)node.MetaFlags & (int)TypeTreeMetaFlags.AnyChildUsesAlignBytes) != 0
            )
            {
                m_Offset = (m_Offset + 3) & ~(3);
            }
        }

        void ProcessArray(TypeTreeNode node, bool isManagedReferenceRegistry = false)
        {
            var dataNode = node.Children[1];

            if (dataNode.IsBasicType)
            {
                var arraySize = m_Reader.ReadInt32(m_Offset);
                m_Crc32 = m_Reader.ComputeCRC(m_Offset, dataNode.Size * arraySize + 4, m_Crc32);
                m_Offset += dataNode.Size * arraySize + 4;
            }
            else
            {
                m_Crc32 = m_Reader.ComputeCRC(m_Offset, 4, m_Crc32);
                var arraySize = m_Reader.ReadInt32(m_Offset);
                m_Offset += 4;

                for (int i = 0; i < arraySize; ++i)
                {
                    if (!isManagedReferenceRegistry)
                    {
                        var size = m_StringBuilder.Length;
                        m_StringBuilder.Append('[');
                        m_StringBuilder.Append(i);
                        m_StringBuilder.Append(']');

                        ProcessNode(dataNode);

                        m_StringBuilder.Remove(size, m_StringBuilder.Length - size);
                    }
                    else
                    {
                        if (dataNode.Children.Count < 3)
                            throw new Exception("Invalid ReferencedObject");

                        // First child is rid.
                        long rid = m_Reader.ReadInt64(m_Offset);
                        m_Crc32 = m_Reader.ComputeCRC(m_Offset, 8, m_Crc32);
                        m_Offset += 8;

                        ProcessManagedReferenceData(dataNode.Children[1], dataNode.Children[2], rid);
                    }
                }
            }
        }

        void ProcessManagedReferenceRegistry(TypeTreeNode node)
        {
            if (node.Children.Count < 2)
                throw new Exception("Invalid ManagedReferenceRegistry");

            // First child is version number.
            var version = m_Reader.ReadInt32(m_Offset);
            m_Crc32 = m_Reader.ComputeCRC(m_Offset, node.Children[0].Size, m_Crc32);
            m_Offset += node.Children[0].Size;

            if (version == 1)
            {
                // Second child is the ReferencedObject.
                var refObjNode = node.Children[1];
                // And its children are the referenced type and data nodes.
                var refTypeNode = refObjNode.Children[0];
                var refObjData = refObjNode.Children[1];

                int i = 0;
                while (ProcessManagedReferenceData(refTypeNode, refObjData, i++))
                {
                }
            }
            else if (version == 2)
            {
                var refIdsVectorNode = node.Children[1];

                if (refIdsVectorNode.Children.Count < 1 || refIdsVectorNode.Name != "RefIds")
                    throw new Exception("Invalid ManagedReferenceRegistry RefIds vector");

                var refIdsArrayNode = refIdsVectorNode.Children[0];

                if (refIdsArrayNode.Children.Count != 2 || !refIdsArrayNode.IsArray)
                    throw new Exception("Invalid ManagedReferenceRegistry RefIds array");

                var size = m_StringBuilder.Length;
                m_StringBuilder.Append('.');
                m_StringBuilder.Append("RefIds");
                ProcessArray(refIdsArrayNode, true);
                m_StringBuilder.Remove(size, m_StringBuilder.Length - size);
            }
            else
            {
                throw new Exception("Unsupported ManagedReferenceRegistry version");
            }
        }

        bool ProcessManagedReferenceData(TypeTreeNode refTypeNode, TypeTreeNode referencedTypeDataNode, long rid)
        {
            if (refTypeNode.Children.Count < 3)
                throw new Exception("Invalid ReferencedManagedType");

            var stringSize = m_Reader.ReadInt32(m_Offset);
            m_Crc32 = m_Reader.ComputeCRC(m_Offset, (int)(m_Offset + stringSize + 4), m_Crc32);
            var className = m_Reader.ReadString(m_Offset + 4, stringSize);
            m_Offset += stringSize + 4;
            m_Offset = (m_Offset + 3) & ~(3);

            stringSize = m_Reader.ReadInt32(m_Offset);
            m_Crc32 = m_Reader.ComputeCRC(m_Offset, (int)(m_Offset + stringSize + 4), m_Crc32);
            var namespaceName = m_Reader.ReadString(m_Offset + 4, stringSize);
            m_Offset += stringSize + 4;
            m_Offset = (m_Offset + 3) & ~(3);

            stringSize = m_Reader.ReadInt32(m_Offset);
            m_Crc32 = m_Reader.ComputeCRC(m_Offset, (int)(m_Offset + stringSize + 4), m_Crc32);
            var assemblyName = m_Reader.ReadString(m_Offset + 4, stringSize);
            m_Offset += stringSize + 4;
            m_Offset = (m_Offset + 3) & ~(3);

            if ((className == "Terminus" && namespaceName == "UnityEngine.DMAT" && assemblyName == "FAKE_ASM") ||
                rid == -1 || rid == -2)
            {
                return false;
            }

            var refTypeTypeTree = m_SerializedFile.GetRefTypeTypeTreeRoot(className, namespaceName, assemblyName);

            // Process the ReferencedObject using its own TypeTree.
            var size = m_StringBuilder.Length;
            m_StringBuilder.Append("rid(");
            m_StringBuilder.Append(rid);
            m_StringBuilder.Append(").data");
            ProcessNode(refTypeTypeTree);
            m_StringBuilder.Remove(size, m_StringBuilder.Length - size);

            return true;
        }

        private void ExtractPPtr(string referencedType)
        {
            var fileId = m_Reader.ReadInt32(m_Offset);
            m_Offset += 4;
            var pathId = m_Reader.ReadInt64(m_Offset);
            m_Offset += 8;

            if (fileId != 0 || pathId != 0)
            {
                var referencedObjectId = m_PPtrResolver.GetObjectId(fileId, pathId);
                m_BuildObjects.AddReference(m_ObjectId, referencedObjectId, m_StringBuilder.ToString() );
                m_PPtrBytes[0] = (byte)(referencedObjectId >> 24);
                m_PPtrBytes[1] = (byte)(referencedObjectId >> 16);
                m_PPtrBytes[2] = (byte)(referencedObjectId >> 8);
                m_PPtrBytes[3] = (byte)(referencedObjectId);
                m_Crc32 = Crc32.Append(m_Crc32, m_PPtrBytes, 0, 4);
            }
        }
    }
}
