using Unity.ProjectAuditor.Editor.UnityFileSystemApi;

namespace Unity.ProjectAuditor.Editor.BuildData.SerializedObjects
{
    public class SerializedObject
    {
        public int Id { get; }
        public long Size { get; protected set; }
        public string Name { get; protected set; }
        public string Type { get; }
        public uint Crc32 { get; }
        public BuildFileInfo BuildFile { get; }
        // This id is required if we want to read the object again.
        public long IdInSerializedFile { get; }

        public SerializedObject(ObjectInfo obj, BuildFileInfo buildFile, PPtrResolver pPtrResolver, TypeTreeReader reader, uint crc32)
        {
            // 0 is the current SerializedFile.
            Id = pPtrResolver.GetObjectId(0, obj.Id);
            IdInSerializedFile = obj.Id;
            Size = reader.Node.Size;
            Type = reader.Node.Type;
            Crc32 = crc32;
            BuildFile = buildFile;

            if (reader.HasChild("m_Name"))
            {
                Name = reader["m_Name"].GetValue<string>();
            }
        }
    }
}
