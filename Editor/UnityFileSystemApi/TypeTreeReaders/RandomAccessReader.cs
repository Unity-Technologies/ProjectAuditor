using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Unity.ProjectAuditor.Editor.UnityFileSystemApi.TypeTreeReaders
{
    // This class should only be used when accessing specific properties of a serialized object and when the object
    // structure is already known. If all the properties of an object must be accessed, the TypeTreeNode should be
    // used instead (see the TextDumper library).
    //
    // Typical usage: randomAccessReader["prop"]["subProp"].GetValue<int>()
    // See the SerializedObjects in the Analyzer library for more examples.
    //
    // This class is optimized to read the least amount of data from the file when accessing properties of a serialized
    // object. It is required because the TypeTree doesn't provide the size of the serialized data when it is
    // variable (e.g. arrays). When accessing a property using this class, the offset of the property in the file is
    // determined by calculating the size of the data that was serialized before it.
    public class RandomAccessReader : IEnumerable<RandomAccessReader>
    {
        SerializedFile m_SerializedFile;
        UnityFileReader m_Reader;
        RandomAccessReader m_LastCachedChild = null;
        Lazy<int> m_Size;
        object m_Value = null;
        Dictionary<string, RandomAccessReader> m_ChildrenCacheObject;
        List<RandomAccessReader> m_ChildrenCacheArray;
        private TypeTreeNode m_TypeTreeNode;

        public int Size => m_Size.Value;
        public long Offset { get; }

        public bool IsObject => !m_TypeTreeNode.IsLeaf && !m_TypeTreeNode.IsBasicType && !m_TypeTreeNode.IsArray;
        public bool IsArrayOfObjects => m_TypeTreeNode.IsArray && !m_TypeTreeNode.Children[1].IsBasicType;
        public bool IsArrayOfBasicTypes => m_TypeTreeNode.IsArray && m_TypeTreeNode.Children[1].IsBasicType;
        public bool IsArray => m_TypeTreeNode.IsArray;

        public RandomAccessReader(SerializedFile serializedFile, TypeTreeNode node, UnityFileReader reader, long offset, bool isReferencedObject = false)
        {
            m_SerializedFile = serializedFile;

            // Special case for vector and map objects, they always have a single Array child so we skip it.
            if (node.Type == "vector" || node.Type == "map" || node.Type == "staticvector")
            {
                m_TypeTreeNode = node.Children[0];
            }
            else
            {
                m_TypeTreeNode = node;
            }

            m_Reader = reader;
            m_Size = new Lazy<int>(GetSize);
            Offset = offset;

            if (IsObject)
            {
                m_ChildrenCacheObject = new Dictionary<string, RandomAccessReader>();

                // ManagedReferenceRegistry must be handled differently because they have 2
                // different versions that are slightly different. The children are manually
                // created and don't match the TypeTree.
                if (m_TypeTreeNode.IsManagedReferenceRegistry)
                {
                    var versionReader = new RandomAccessReader(m_SerializedFile, node.Children[0], reader, offset);
                    m_ChildrenCacheObject["version"] = versionReader;
                    int version = versionReader.GetValue<int>();
                    long curOffset = versionReader.Offset + versionReader.Size;

                    if (version == 1)
                    {
                        // Second child is the ReferencedObject.
                        var refObjNode = node.Children[1];
                        int i = 0;

                        // In version 1, we don't know how many referenced objects there are.
                        do
                        {
                            // Create the referenced object reader.
                            var refObjReader = new RandomAccessReader(m_SerializedFile, refObjNode, reader, curOffset, true);

                            // A referenced object with null data means that we reached the end of the referenced objects.
                            if (refObjReader["data"] == null)
                            {
                                break;
                            }

                            // Add the reader to cache.
                            m_ChildrenCacheObject[$"rid({i++})"] = refObjReader;
                            curOffset += refObjReader.Size;
                        } while (true);
                    }
                    else if (version == 2)
                    {
                        // In version 2, referenced objects are stored in a vector.

                        // Second child is the RefIds vector.
                        var refIdsVectorNode = node.Children[1];
                        // RefIds vector's child is the Array.
                        var refIdsArrayNode = refIdsVectorNode.Children[0];

                        // First child is the array size.
                        int arraySize = m_Reader.ReadInt32(curOffset);
                        curOffset += 4;

                        // Second child is the ReferencedObject.
                        var refObjNode = refIdsArrayNode.Children[1];

                        for (int i = 0; i < arraySize; ++i)
                        {
                            // Create and cache the referenced object.
                            var refObjReader = new RandomAccessReader(m_SerializedFile, refObjNode, reader, curOffset, true);
                            m_ChildrenCacheObject[$"rid({refObjReader["rid"].GetValue<long>()})"] = refObjReader;
                            curOffset += refObjReader.Size;
                        }
                    }
                    else
                    {
                        throw new Exception("Unsupported ManagedReferenceRegistry version");
                    }
                }
                else if (isReferencedObject)
                {
                    if (HasChild("rid"))
                    {
                        var rid = GetChild("rid").GetValue<long>();

                        // -1 is unknown and -2 is null.
                        if (rid == -1 || rid == -2)
                        {
                            m_ChildrenCacheObject["data"] = null;
                            return;
                        }
                    }

                    var referencedManagedType = GetChild("type");

                    string className = referencedManagedType["class"].GetValue<string>();
                    string namespaceName = referencedManagedType["ns"].GetValue<string>();
                    string assemblyName = referencedManagedType["asm"].GetValue<string>();

                    // This is the end marker.
                    if (className == "Terminus" && namespaceName == "UnityEngine.DMAT" && assemblyName == "FAKE_ASM")
                    {
                        // Set data to null to signal the end.
                        m_ChildrenCacheObject["data"] = null;
                    }
                    else
                    {
                        var refTypeRoot = m_SerializedFile.GetRefTypeTypeTreeRoot(className, namespaceName, assemblyName);

                        // Manually create and cache a reader for the referenced type data, using its own TypeTree.
                        var refTypeDataReader = new RandomAccessReader(m_SerializedFile, refTypeRoot, reader,
                            referencedManagedType.Offset + referencedManagedType.Size);
                        m_ChildrenCacheObject["data"] = refTypeDataReader;
                    }
                }
            }
        }

        public bool HasChild(string name)
        {
            // Special case for ManagedReferenceRegistry. The children are in cache and do not match the TypeTreeNode.
            return m_TypeTreeNode.IsManagedReferenceRegistry ? m_ChildrenCacheObject.ContainsKey(name) :
                m_TypeTreeNode.Children.Find(n => n.Name == name) != null;
        }

        int GetSize()
        {
            int size;

            if (m_TypeTreeNode.IsBasicType)
            {
                size = m_TypeTreeNode.Size;
            }
            else if (m_TypeTreeNode.IsArray)
            {
                var dataNode = m_TypeTreeNode.Children[1];

                if (dataNode.IsBasicType)
                {
                    var arraySize = m_Reader.ReadInt32(Offset);
                    size = dataNode.Size * arraySize;
                    size += 4;
                }
                else
                {
                    var arraySize = GetArraySize();
                    if (arraySize > 0)
                    {
                        if (dataNode.HasConstantSize)
                        {
                            size = GetArrayElement(0).Size * arraySize;
                            size += 4;
                        }
                        else
                        {
                            var lastArrayElement = GetArrayElement(arraySize - 1);
                            size = (int)(lastArrayElement.Offset + lastArrayElement.Size - Offset);
                        }
                    }
                    else
                    {
                        size = 4;
                    }
                }
            }
            else if (m_TypeTreeNode.CSharpType == typeof(string))
            {
                size = m_Reader.ReadInt32(Offset) + 4;
            }
            else
            {
                var lastChild = GetChild(m_TypeTreeNode.Children.Last().Name);
                size = (int)(lastChild.Offset + lastChild.Size - Offset);
            }

            if (
                ((int)m_TypeTreeNode.MetaFlags & (int)TypeTreeMetaFlags.AlignBytes) != 0 ||
                ((int)m_TypeTreeNode.MetaFlags & (int)TypeTreeMetaFlags.AnyChildUsesAlignBytes) != 0
            )
            {
                var endOffset = (Offset + size + 3) & ~(3);

                size = (int)(endOffset - Offset);
            }

            return size;
        }

        RandomAccessReader GetChild(string name)
        {
            if (m_ChildrenCacheObject == null)
                throw new InvalidOperationException("Node is not an object");

            if (m_ChildrenCacheObject.TryGetValue(name, out var nodeReader))
                return nodeReader;

            if (m_TypeTreeNode.IsManagedReferenceRegistry)
            {
                // ManagedReferenceRegistry are handled differently. The children
                // are all cached at construction time.
                throw new KeyNotFoundException();
            }

            long offset;
            if (m_LastCachedChild == null)
            {
                offset = Offset;
            }
            else
            {
                offset = m_LastCachedChild.Offset + m_LastCachedChild.Size;
            }

            for (int i = m_ChildrenCacheObject.Count; i < m_TypeTreeNode.Children.Count; ++i)
            {
                var child = m_TypeTreeNode.Children[i];

                nodeReader = new RandomAccessReader(m_SerializedFile, child, m_Reader, offset);
                m_ChildrenCacheObject.Add(child.Name, nodeReader);
                m_LastCachedChild = nodeReader;

                if (name == child.Name)
                    return nodeReader;

                offset += nodeReader.Size;
            }

            throw new KeyNotFoundException();
        }

        public int GetArraySize()
        {
            if (m_ChildrenCacheArray == null)
            {
                if (!IsArrayOfObjects)
                {
                    if (m_TypeTreeNode.IsArray)
                    {
                        return m_Reader.ReadInt32(Offset);
                    }

                    throw new InvalidOperationException("Node is not an array");
                }

                var arraySize = m_Reader.ReadInt32(Offset);
                m_ChildrenCacheArray = new List<RandomAccessReader>(arraySize);
            }

            return m_ChildrenCacheArray.Capacity;
        }

        RandomAccessReader GetArrayElement(int index)
        {
            RandomAccessReader nodeReader = null;
            var arraySize = GetArraySize();

            if (index < m_ChildrenCacheArray.Count)
                return m_ChildrenCacheArray[index];

            long offset;
            if (m_LastCachedChild == null)
            {
                offset = Offset + 4; // 4 is the array size.
            }
            else
            {
                offset = m_LastCachedChild.Offset + m_LastCachedChild.Size;
            }

            var dataNode = m_TypeTreeNode.Children[1];

            for (int i = m_ChildrenCacheArray.Count; i < arraySize; ++i)
            {
                nodeReader = new RandomAccessReader(m_SerializedFile, dataNode, m_Reader, offset);
                m_ChildrenCacheArray.Add(nodeReader);
                m_LastCachedChild = nodeReader;

                if (index == i)
                    return nodeReader;

                offset += nodeReader.Size;
            }

            throw new IndexOutOfRangeException();
        }

        public int Count => IsArrayOfObjects ? GetArraySize() : (IsObject ? m_TypeTreeNode.Children.Count : 0);

        public RandomAccessReader this[string name] => GetChild(name);

        public RandomAccessReader this[int index] => GetArrayElement(index);

        public T GetValue<T>()
        {
            if (m_Value == null)
            {
                switch (Type.GetTypeCode(m_TypeTreeNode.CSharpType))
                {
                    case TypeCode.Int32:
                        m_Value = m_Reader.ReadInt32(Offset);
                        break;

                    case TypeCode.UInt32:
                        m_Value = m_Reader.ReadUInt32(Offset);
                        break;

                    case TypeCode.Single:
                        m_Value = m_Reader.ReadFloat(Offset);
                        break;

                    case TypeCode.Double:
                        m_Value = m_Reader.ReadDouble(Offset);
                        break;

                    case TypeCode.Int16:
                        m_Value = m_Reader.ReadInt16(Offset);
                        break;

                    case TypeCode.UInt16:
                        m_Value = m_Reader.ReadUInt16(Offset);
                        break;

                    case TypeCode.Int64:
                        m_Value = m_Reader.ReadInt64(Offset);
                        break;

                    case TypeCode.UInt64:
                        m_Value = m_Reader.ReadUInt64(Offset);
                        break;

                    case TypeCode.SByte:
                        m_Value = m_Reader.ReadUInt8(Offset);
                        break;

                    case TypeCode.Byte:
                    case TypeCode.Char:
                        m_Value = m_Reader.ReadUInt8(Offset);
                        break;

                    case TypeCode.Boolean:
                        m_Value = (m_Reader.ReadUInt8(Offset) != 0);
                        break;

                    case TypeCode.String:
                        var stringSize = m_Reader.ReadInt32(Offset);
                        m_Value = m_Reader.ReadString(Offset + 4, stringSize);
                        break;

                    default:
                        if (typeof(T).IsArray)
                        {
                            m_Value = ReadBasicTypeArray();
                            return (T)m_Value;
                        }

                        throw new Exception($"Can't get value of {m_TypeTreeNode.Type} type");
                }
            }

            return (T)Convert.ChangeType(m_Value, typeof(T));
        }

        Array ReadBasicTypeArray()
        {
            var arraySize = m_Reader.ReadInt32(Offset);
            var elementNode = m_TypeTreeNode.Children[1];

            // Special case for boolean arrays.
            if (elementNode.CSharpType == typeof(bool))
            {
                var tmpArray = new byte[arraySize];
                var boolArray = new bool[arraySize];

                m_Reader.ReadArray(Offset + 4, arraySize * elementNode.Size, tmpArray);

                for (int i = 0; i < arraySize; ++i)
                {
                    boolArray[i] = tmpArray[i] != 0;
                }

                return boolArray;
            }
            else
            {
                var array = Array.CreateInstance(elementNode.CSharpType, arraySize);

                m_Reader.ReadArray(Offset + 4, arraySize * elementNode.Size, array);

                return array;
            }
        }

        class Enumerator : IEnumerator<RandomAccessReader>
        {
            int m_Index = -1;
            RandomAccessReader m_NodeReader;

            public Enumerator(RandomAccessReader nodeReader)
            {
                m_NodeReader = nodeReader;
            }

            public RandomAccessReader Current
            {
                get
                {
                    if (m_NodeReader.IsObject)
                    {
                        return m_NodeReader.GetChild(m_NodeReader.m_TypeTreeNode.Children[m_Index].Name);
                    }
                    else if (m_NodeReader.IsArrayOfObjects)
                    {
                        return m_NodeReader.GetArrayElement(m_Index);
                    }

                    throw new InvalidOperationException();
                }
            }

            object IEnumerator.Current => Current;

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                ++m_Index;
                return m_Index < m_NodeReader.Count;
            }

            public void Reset()
            {
                m_Index = -1;
            }
        }

        public IEnumerator<RandomAccessReader> GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }
    }
}
