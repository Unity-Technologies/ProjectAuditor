using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.UnityFileSystemApi;

namespace Unity.ProjectAuditor.Editor.BuildData.SerializedObjects
{
    public class PreloadData : SerializedObject
    {
        public IReadOnlyList<PPtr> Assets { get; }

        public PreloadData(ObjectInfo obj, BuildFileInfo buildFile, PPtrResolver pPtrResolver, TypeTreeReader reader, uint crc32)
            : base(obj, buildFile, pPtrResolver, reader, crc32)
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
