using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.MonoBehaviourCollectors
{
    public interface IPrefabCollector
    {
        public bool TryCollectPrefabReference(object obj, HashSet<string> prefabs);
    }

    internal class GameObjectPrefabCollector : IPrefabCollector
    {
        public bool TryCollectPrefabReference(object obj, HashSet<string> prefabs)
        {
            if (obj == null || !(obj is GameObject))
                return false;

            GameObject go = obj as GameObject;

            if (go == null)
                return false;

            if (prefabs == null)
                return false;

            if (PrefabUtility.IsPartOfPrefabAsset(go))
            {
                var assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go);
                prefabs.Add(assetPath);
                return true;
            }

            return false;
        }
    }
}
