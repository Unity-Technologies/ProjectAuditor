using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Unity.ProjectAuditor.Editor.MonoBehaviourCollectors
{
    public class AssetReferencePrefabCollector : IPrefabCollector
    {
        public bool TryCollectPrefabReference(object obj, HashSet<string> prefabs)
        {
            if (obj == null || !(obj is AssetReference))
                return false;

            AssetReference refValue = obj as AssetReference;

            var asset = refValue.editorAsset as GameObject;

            if (asset == null)
                return false;

            if (PrefabUtility.IsPartOfPrefabAsset(asset))
            {
                var assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(asset);
                prefabs.Add(assetPath);
                return true;
            }

            return false;
        }
    }
}
