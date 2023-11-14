using System.Collections.Generic;
using usingUnity.ProjectAuditor.Editor.UnityFileSystemApi.TypeTreeReaders;

namespace Unity.ProjectAuditor.Editor.Analyzer.SerializedObjects
{
    public class AssetBundle
    {
        private string m_Name;
        public string Name => m_Name;
        private List<Asset> m_Assets;
        public IReadOnlyList<Asset> Assets => m_Assets;
        private List<PPtr> m_PreloadTable;
        public IReadOnlyList<PPtr> PreloadTable => m_PreloadTable;
        private bool m_IsSceneAssetBundle;
        public bool IsSceneAssetBundle => m_IsSceneAssetBundle;

        public class Asset
        {
            private string m_Name;
            public string Name => m_Name;
            private PPtr m_PPtr;
            public PPtr PPtr => m_PPtr;
            private int m_PreloadIndex;
            public int PreloadIndex => m_PreloadIndex;
            private int m_PreloadSize;
            public int PreloadSize => m_PreloadSize;

            public Asset(RandomAccessReader reader)
            {
                m_Name = reader["first"].GetValue<string>();
                m_PPtr = new PPtr(reader["second"]["asset"]);
                m_PreloadIndex = reader["second"]["preloadIndex"].GetValue<int>();
                m_PreloadSize = reader["second"]["preloadSize"].GetValue<int>();
            }
        }

        public AssetBundle(RandomAccessReader reader)
        {
            m_Name = reader["m_Name"].GetValue<string>();
            m_Assets = new List<Asset>(reader["m_Container"].GetArraySize());
            m_PreloadTable = new List<PPtr>(reader["m_PreloadTable"].GetArraySize());
            m_IsSceneAssetBundle = reader["m_IsStreamedSceneAssetBundle"].GetValue<bool>();

            foreach (var pptr in reader["m_PreloadTable"])
            {
                m_PreloadTable.Add(new PPtr(pptr));
            }

            foreach (var asset in reader["m_Container"])
            {
                m_Assets.Add(new Asset(asset));
            }
        }
    }
}
