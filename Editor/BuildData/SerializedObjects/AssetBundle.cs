using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.UnityFileSystemApi.TypeTreeReaders;

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

            public Asset(RandomAccessReader reader)
            {
                Name = reader["first"].GetValue<string>();
                PPtr = new PPtr(reader["second"]["asset"]);
                PreloadIndex = reader["second"]["preloadIndex"].GetValue<int>();
                PreloadSize = reader["second"]["preloadSize"].GetValue<int>();
            }
        }

        public AssetBundle(RandomAccessReader reader, long size, BuildFileInfo buildFile)
            : base(reader, size, "AssetBundle", buildFile)
        {
            var assets = new List<Asset>(reader["m_Container"].GetArraySize());
            var preloadTable = new List<PPtr>(reader["m_PreloadTable"].GetArraySize());
            IsSceneAssetBundle = reader["m_IsStreamedSceneAssetBundle"].GetValue<bool>();

            foreach (var pptr in reader["m_PreloadTable"])
            {
                preloadTable.Add(new PPtr(pptr));
            }

            PreloadTable = preloadTable;

            foreach (var asset in reader["m_Container"])
            {
                assets.Add(new Asset(asset));
            }

            Assets = assets;
        }
    }
}
