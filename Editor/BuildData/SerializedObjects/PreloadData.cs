using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.UnityFileSystemApi;

namespace Unity.ProjectAuditor.Editor.BuildData.SerializedObjects
{
    public class PreloadData : SerializedObject
    {
        public IReadOnlyList<PPtr> Assets { get; }

        public PreloadData(BuildFileInfo buildFile, PPtrResolver pPtrResolver, TypeTreeReader reader, int id, long size, uint crc32)
            : base(buildFile, pPtrResolver, reader, id, size, crc32, "PreloadData")
        {
            var assets = new List<PPtr>(reader["m_Assets"].GetArraySize());

            foreach (var pptr in reader["m_Assets"])
            {
                assets.Add(new PPtr(pPtrResolver, pptr));
            }

            Assets = assets;
        }
    }
}
