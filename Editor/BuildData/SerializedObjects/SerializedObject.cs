using Unity.ProjectAuditor.Editor.UnityFileSystemApi;

namespace Unity.ProjectAuditor.Editor.BuildData.SerializedObjects
{
    public class SerializedObject
    {
        public int Id { get; }
        public long Size { get; protected set; }
        public string Name { get; protected set; }
        public string Type { get; }
        public BuildFileInfo BuildFile { get; }

        public SerializedObject(BuildFileInfo buildFile, PPtrResolver pPtrResolver, TypeTreeReader reader, int id, long size, string type)
        {
            Id = id;
            Size = size;
            Type = type;
            BuildFile = buildFile;

            if (reader.HasChild("m_Name"))
            {
                Name = reader["m_Name"].GetValue<string>();
            }
        }
    }
}
