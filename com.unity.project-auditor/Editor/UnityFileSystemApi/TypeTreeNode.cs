using System;
using System.Collections.Generic;
using System.Text;

namespace Unity.ProjectAuditor.Editor.UnityFileSystemApi
{
    // A TypeTreeNode represents how a property of a serialized object was written to disk.
    // See the TextDumper library for an example.
    public class TypeTreeNode
    {
        int m_FirstChildNodeIndex;
        int m_NextNodeIndex;
        TypeTreeHandle m_Handle;
        Lazy<List<TypeTreeNode>> m_Children;
        Lazy<Type> m_CSharpType;
        Lazy<bool> m_hasConstantSize;

        // The type of the property (basic type like int or float or class names for objects)
        public readonly string Type;
        // The name of the property (e.g. m_IndexBuffer for a Mesh or m_Width for a Texture)
        public readonly string Name;
        // The size of the property (for basic types only, otherwise -1)
        public readonly int Size;
        // The offset of the property (mostly useless).
        public readonly int Offset;
        // Flags used for different things (e.g. field is an array, data alignment, etc.)
        public readonly TypeTreeFlags Flags;
        public readonly TypeTreeMetaFlags MetaFlags;

        // Child nodes container.
        public List<TypeTreeNode> Children => m_Children.Value;

        // True if the field has no child.
        public bool IsLeaf => m_FirstChildNodeIndex == 0;

        // True if the field is a basic type. (int, float, char, etc.)
        public bool IsBasicType => IsLeaf && Size > 0;

        // True if the field is an array.
        public bool IsArray => ((int)Flags & (int)TypeTreeFlags.IsArray) != 0;

        // True if the field is a ManagedReferenceRegistry
        public bool IsManagedReferenceRegistry => ((int)Flags & (int)TypeTreeFlags.IsManagedReferenceRegistry) != 0;

        // C# type corresponding to the node type
        public Type CSharpType => m_CSharpType.Value;

        // True if the node has a constant size (it contains no array or other containers with variable size).
        public bool HasConstantSize => m_hasConstantSize.Value;

        [ThreadStatic]
        static StringBuilder s_NodeType;
        [ThreadStatic]
        static StringBuilder s_NodeName;

        // Properties are required to initialize the ThreadStatic members.
        static StringBuilder NodeTypeBuilder
        {
            get
            {
                if (s_NodeType == null)
                {
                    s_NodeType = new StringBuilder(512);
                }
                return s_NodeType;
            }
        }

        static StringBuilder NodeNameBuilder
        {
            get
            {
                if (s_NodeName == null)
                {
                    s_NodeName = new StringBuilder(512);
                }
                return s_NodeName;
            }
        }

        internal TypeTreeNode(TypeTreeHandle typeTreeHandle, int nodeIndex)
        {
            m_Handle = typeTreeHandle;

            var r = DllWrapper.GetTypeTreeNodeInfo(m_Handle, nodeIndex, NodeTypeBuilder, NodeTypeBuilder.Capacity, NodeNameBuilder, NodeNameBuilder.Capacity, out Offset, out Size, out Flags, out MetaFlags, out m_FirstChildNodeIndex, out m_NextNodeIndex);
            UnityFileSystem.HandleErrors(r);

            Type = NodeTypeBuilder.ToString();
            Name = NodeNameBuilder.ToString();

            m_Children = new Lazy<List<TypeTreeNode>>(GetChildren);
            m_CSharpType = new Lazy<Type>(GetCSharpType);
            m_hasConstantSize = new Lazy<bool>(GetHasConstantSize);
        }

        internal List<TypeTreeNode> GetChildren()
        {
            var children = new List<TypeTreeNode>();
            var current = m_FirstChildNodeIndex;

            while (current != 0)
            {
                var child = new TypeTreeNode(m_Handle, current);
                children.Add(child);
                current = child.m_NextNodeIndex;
            }

            return children;
        }

        bool GetHasConstantSize()
        {
            if (IsArray || CSharpType == typeof(string))
                return false;

            foreach (var child in Children)
            {
                if (!child.HasConstantSize)
                    return false;
            }

            return true;
        }

        Type GetCSharpType()
        {
            switch (Type)
            {
                case "int":
                case "SInt32":
                case "TypePtr":
                    return typeof(int);

                case "unsigned int":
                case "UInt32":
                    return typeof(uint);

                case "float":
                    return typeof(float);

                case "double":
                    return typeof(double);

                case "SInt16":
                    return typeof(short);

                case "UInt16":
                    return typeof(ushort);

                case "SInt64":
                    return typeof(long);

                case "FileSize":
                case "UInt64":
                    return typeof(ulong);

                case "SInt8":
                    return typeof(sbyte);

                case "UInt8":
                case "char":
                    return typeof(byte);

                case "bool":
                    return typeof(bool);

                case "string":
                    return typeof(string);

                default:
                {
                    if (Size == 8)
                    {
                        return typeof(long);
                    }
                    else if (Size == 4)
                    {
                        return typeof(int);
                    }
                    else if (Size == 2)
                    {
                        return typeof(short);
                    }
                    else if (Size == 1)
                    {
                        return typeof(sbyte);
                    }

                    return typeof(object);
                }
            }

            throw new Exception($"Unknown type {Type}");
        }
    }
}
