using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Unity.ProjectAuditor.Editor.UnityFileSystemApi
{
    internal class UnityArchiveHandle : SafeHandle
    {
        public UnityArchiveHandle() : base(IntPtr.Zero, true)
        {
        }

        public override bool IsInvalid => handle == IntPtr.Zero;

        protected override bool ReleaseHandle()
        {
            return DllWrapper.UnmountArchive(handle) == ReturnCode.Success;
        }
    }

    internal class UnityFileHandle : SafeHandle
    {
        public UnityFileHandle() : base(IntPtr.Zero, true)
        {
        }

        public override bool IsInvalid => handle == IntPtr.Zero;

        protected override bool ReleaseHandle()
        {
            return DllWrapper.CloseFile(handle) == ReturnCode.Success;
        }
    }

    internal class SerializedFileHandle : SafeHandle
    {
        public SerializedFileHandle() : base(IntPtr.Zero, true)
        {
        }

        public override bool IsInvalid => handle == IntPtr.Zero;

        protected override bool ReleaseHandle()
        {
            return DllWrapper.CloseSerializedFile(handle) == ReturnCode.Success;
        }
    }

    internal class TypeTreeHandle : SafeHandle
    {
        public TypeTreeHandle() : base(IntPtr.Zero, true)
        {
        }

        public override bool IsInvalid => handle == IntPtr.Zero;

        protected override bool ReleaseHandle()
        {
            return true;
        }

        internal IntPtr Handle => handle;
    }

    internal enum ReturnCode
    {
        Success,
        AlreadyInitialized,
        NotInitialized,
        FileNotFound,
        FileFormatError,
        InvalidArgument,
        HigherSerializedFileVersion,
        DestinationBufferTooSmall,
        InvalidObjectId,
        UnknownError,
        FileError,
        ErrorCreatingArchiveFile,
        ErrorAddingFileToArchive,
        TypeNotFound,
    }

    [Flags]
    public enum ArchiveNodeFlags
    {
        None            = 0,
        Directory       = 1 << 0,
        Deleted         = 1 << 1,
        SerializedFile  = 1 << 2,
    }

    public enum CompressionType
    {
        None,
        Lzma,
        Lz4,
        Lz4HC,
    };

    public enum SeekOrigin
    {
        Begin,
        Current,
        End,
    }

    public enum ExternalReferenceType
    {
        NonAssetType,
        DeprecatedCachedAssetType,
        SerializedAssetType,
        MetaAssetType,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ObjectInfo
    {
        public readonly long Id;
        public readonly long Offset;
        public readonly long Size;
        public readonly int TypeId;
    }
    [Flags]
    public enum TypeTreeFlags
    {
        None                        = 0,
        IsArray                     = 1 << 0,
        IsManagedReference          = 1 << 1,
        IsManagedReferenceRegistry  = 1 << 2,
        IsArrayOfRefs               = 1 << 3,
    }

    [Flags]
    public enum TypeTreeMetaFlags
    {
        None                    = 0,
        AlignBytes              = 1 << 14,
        AnyChildUsesAlignBytes  = 1 << 15,
    }

    internal static class DllWrapper
    {
        [DllImport("UnityFileSystemApi",
            CallingConvention = CallingConvention.Cdecl,
            EntryPoint = "UFS_Init")]
        public static extern ReturnCode Init();

        [DllImport("UnityFileSystemApi",
            CallingConvention = CallingConvention.Cdecl,
            EntryPoint = "UFS_Cleanup")]
        public static extern ReturnCode Cleanup();

        [DllImport("UnityFileSystemApi",
            CallingConvention = CallingConvention.Cdecl,
            EntryPoint = "UFS_MountArchive")]
        public static extern ReturnCode MountArchive([MarshalAs(UnmanagedType.LPStr)] string path, [MarshalAs(UnmanagedType.LPStr)] string mountPoint, out UnityArchiveHandle handle);

        [DllImport("UnityFileSystemApi",
            CallingConvention = CallingConvention.Cdecl,
            EntryPoint = "UFS_UnmountArchive")]
        public static extern ReturnCode UnmountArchive(IntPtr handle);

        [DllImport("UnityFileSystemApi",
            CallingConvention = CallingConvention.Cdecl,
            EntryPoint = "UFS_GetArchiveNodeCount")]
        public static extern ReturnCode GetArchiveNodeCount(UnityArchiveHandle handle, out int count);

        [DllImport("UnityFileSystemApi",
            CallingConvention = CallingConvention.Cdecl,
            EntryPoint = "UFS_GetArchiveNode")]
        public static extern ReturnCode GetArchiveNode(UnityArchiveHandle handle, int nodeIndex, StringBuilder path, int pathLen, out long size, out ArchiveNodeFlags flags);

        [DllImport("UnityFileSystemApi",
            CallingConvention = CallingConvention.Cdecl,
            EntryPoint = "UFS_CreateArchive")]
        public static extern ReturnCode CreateArchive([MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPStr)] string[] sourceFiles,
            [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPStr)] string[] aliases, bool[] isSerializedFile, int count,
            [MarshalAs(UnmanagedType.LPStr)] string archiveFile, CompressionType compression, out int crc);

        [DllImport("UnityFileSystemApi",
            CallingConvention = CallingConvention.Cdecl,
            EntryPoint = "UFS_OpenFile")]
        public static extern ReturnCode OpenFile([MarshalAs(UnmanagedType.LPStr)] string path, out UnityFileHandle handle);

        [DllImport("UnityFileSystemApi",
            CallingConvention = CallingConvention.Cdecl, EntryPoint = "UFS_ReadFile")]
        public static extern ReturnCode ReadFile(UnityFileHandle handle, long size,
            [MarshalAs(UnmanagedType.LPArray)] byte[] buffer, out long actualSize);

        [DllImport("UnityFileSystemApi",
            CallingConvention = CallingConvention.Cdecl,
            EntryPoint = "UFS_SeekFile")]
        public static extern ReturnCode SeekFile(UnityFileHandle handle, long offset, SeekOrigin origin, out long newPosition);

        [DllImport("UnityFileSystemApi",
            CallingConvention = CallingConvention.Cdecl,
            EntryPoint = "UFS_GetFileSize")]
        public static extern ReturnCode GetFileSize(UnityFileHandle handle, out long size);

        [DllImport("UnityFileSystemApi",
            CallingConvention = CallingConvention.Cdecl,
            EntryPoint = "UFS_CloseFile")]
        public static extern ReturnCode CloseFile(IntPtr handle);

        [DllImport("UnityFileSystemApi",
            CallingConvention = CallingConvention.Cdecl,
            EntryPoint = "UFS_OpenSerializedFile")]
        public static extern ReturnCode OpenSerializedFile([MarshalAs(UnmanagedType.LPStr)] string path, out SerializedFileHandle handle);

        [DllImport("UnityFileSystemApi",
            CallingConvention = CallingConvention.Cdecl,
            EntryPoint = "UFS_CloseSerializedFile")]
        public static extern ReturnCode CloseSerializedFile(IntPtr handle);

        [DllImport("UnityFileSystemApi",
            CallingConvention = CallingConvention.Cdecl,
            EntryPoint = "UFS_GetExternalReferenceCount")]
        public static extern ReturnCode GetExternalReferenceCount(SerializedFileHandle handle, out int count);

        [DllImport("UnityFileSystemApi",
            CallingConvention = CallingConvention.Cdecl,
            EntryPoint = "UFS_GetExternalReference")]
        public static extern ReturnCode GetExternalReference(SerializedFileHandle handle, int index, StringBuilder path, int pathLen, StringBuilder guid, out ExternalReferenceType type);

        [DllImport("UnityFileSystemApi",
            CallingConvention = CallingConvention.Cdecl,
            EntryPoint = "UFS_GetObjectCount")]
        public static extern ReturnCode GetObjectCount(SerializedFileHandle handle, out int count);

        [DllImport("UnityFileSystemApi",
            CallingConvention = CallingConvention.Cdecl,
            EntryPoint = "UFS_GetObjectInfo")]
        public static extern ReturnCode GetObjectInfo(SerializedFileHandle handle, [In, Out] ObjectInfo[] objectData, int len);

        [DllImport("UnityFileSystemApi",
            CallingConvention = CallingConvention.Cdecl,
            EntryPoint = "UFS_GetTypeTree")]
        public static extern ReturnCode GetTypeTree(SerializedFileHandle handle, long objectId, out TypeTreeHandle typeTree);

        [DllImport("UnityFileSystemApi",
            CallingConvention = CallingConvention.Cdecl,
            EntryPoint = "UFS_GetRefTypeTypeTree")]
        public static extern ReturnCode GetRefTypeTypeTree(SerializedFileHandle handle, [MarshalAs(UnmanagedType.LPStr)] string className,
            [MarshalAs(UnmanagedType.LPStr)] string namespaceName, [MarshalAs(UnmanagedType.LPStr)] string assemblyName, out TypeTreeHandle typeTree);

        [DllImport("UnityFileSystemApi",
            CallingConvention = CallingConvention.Cdecl,
            EntryPoint = "UFS_GetTypeTreeNodeInfo")]
        public static extern ReturnCode GetTypeTreeNodeInfo(TypeTreeHandle handle, int node, StringBuilder type, int typeLen,
            StringBuilder name, int nameLen, out int offset, out int size, [MarshalAs(UnmanagedType.U4)] out TypeTreeFlags flags,
            [MarshalAs(UnmanagedType.U4)] out TypeTreeMetaFlags metaFlags, out int firstChildNode,
            out int nextNode);
    }
}
