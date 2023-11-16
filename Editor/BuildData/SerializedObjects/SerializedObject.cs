using Unity.ProjectAuditor.Editor.UnityFileSystemApi.TypeTreeReaders;

namespace Unity.ProjectAuditor.Editor.BuildData.SerializedObjects
{
    public class SerializedObject
    {
        public long Size { get; protected set; }
        public string Name { get; protected set; }
        public string Type { get; }
        public BuildFileInfo BuildFile { get; }

        public SerializedObject(RandomAccessReader reader, long size, string type, BuildFileInfo buildFile)
        {
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
