using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.UnityFileSystemApi;

namespace Unity.ProjectAuditor.Editor.BuildData.SerializedObjects
{
    public class AssetBundle : SerializedObject
    {
        public IReadOnlyList<Asset> Assets { get; }
        public IReadOnlyList<PPtr> PreloadTable { get; }
        public bool IsSceneAssetBundle { get; }

        public class Asset
        {
            public string Name { get; }
            public PPtr PPtr { get; }
            public int PreloadIndex { get; }
            public int PreloadSize { get; }

            public Asset(PPtrResolver pPtrResolver, TypeTreeReader reader)
            {
                Name = reader["first"].GetValue<string>();
                PPtr = new PPtr(pPtrResolver, reader["second"]["asset"]);
                PreloadIndex = reader["second"]["preloadIndex"].GetValue<int>();
                PreloadSize = reader["second"]["preloadSize"].GetValue<int>();
            }
        }

        public AssetBundle(ObjectInfo obj, BuildFileInfo buildFile, PPtrResolver pPtrResolver, TypeTreeReader reader, uint crc32)
            : base(obj, buildFile, pPtrResolver, reader, crc32)
        {
            var assets = new List<Asset>(reader["m_Container"].GetArraySize());
            var preloadTable = new List<PPtr>(reader["m_PreloadTable"].GetArraySize());
            IsSceneAssetBundle = reader["m_IsStreamedSceneAssetBundle"].GetValue<bool>();

            foreach (var pptr in reader["m_PreloadTable"])
            {
                preloadTable.Add(new PPtr(pPtrResolver, pptr));
            }

            PreloadTable = preloadTable;

            foreach (var asset in reader["m_Container"])
            {
                assets.Add(new Asset(pPtrResolver, asset));
            }

            Assets = assets;
        }
    }
}
