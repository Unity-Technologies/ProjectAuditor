using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.UnityFileSystemApi.TypeTreeReaders;

namespace Unity.ProjectAuditor.Editor.BuildData.SerializedObjects
{
    public class PreloadData : SerializedObject
    {
        public IReadOnlyList<PPtr> Assets { get; }

        public PreloadData(RandomAccessReader reader, long size, BuildFileInfo buildFile)
            : base(reader, size, "PreloadData", buildFile)
        {
            var assets = new List<PPtr>(reader["m_Assets"].GetArraySize());

            foreach (var pptr in reader["m_Assets"])
            {
                assets.Add(new PPtr(pptr));
            }

            Assets = assets;
        }
    }
}
