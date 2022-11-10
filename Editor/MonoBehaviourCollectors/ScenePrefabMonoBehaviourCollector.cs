using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.ProjectAuditor.Editor.MonoBehaviourCollectors
{
    public class ScenePrefabMonoBehaviourCollector
    {
        HashSet<string> m_Prefabs = new HashSet<string>();
        Dictionary<string, int> m_MonoBehaviourCounts = new Dictionary<string, int>();

        public Dictionary<string, int> MonoBehaviourCounts
        {
            get { return m_MonoBehaviourCounts; }
        }

        List<IPrefabCollector> m_PrefabCollectors = new List<IPrefabCollector>();

        void SetupPrefabScanners()
        {
            m_PrefabCollectors.Clear();

            foreach (var type in TypeCache.GetTypesDerivedFrom(typeof(IPrefabCollector)))
            {
                var instance = Activator.CreateInstance(type) as IPrefabCollector;
                m_PrefabCollectors.Add(instance);
            }
        }

        public void Collect(Scene scene)
        {
            SetupPrefabScanners();

            m_Prefabs.Clear();

            // 1. Deep scan of scene for Prefab references (GameObject, AssetReference)
            var rootGameObjects = scene.GetRootGameObjects();
            foreach (var go in rootGameObjects)
            {
                CollectPrefabReferencesRecursive(go);
            }

            while (true)
            {
                int prevCount = m_Prefabs.Count;

                var oldPrefabs = m_Prefabs.ToArray();

                foreach (var prefabPath in oldPrefabs)
                {
                    var prefabGo = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

                    CollectPrefabReferencesRecursive(prefabGo);
                }

                if (prevCount == m_Prefabs.Count)
                    break;
            }

            // 2. Collect MonoBehaviours found on any Prefabs found above
            foreach (var prefabPath in m_Prefabs)
            {
                var prefabGo = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

                CollectMonoBehavioursFromPrefabRecursive(prefabGo);
            }
        }

        void CollectPrefabReferencesRecursive(GameObject go)
        {
            var monoBehaviours = go.GetComponents<MonoBehaviour>();
            foreach (var mono in monoBehaviours)
            {
                CollectPrefabReferencesFromFields(mono);
            }

            foreach (Transform child in go.transform)
            {
                CollectPrefabReferencesRecursive(child.gameObject);
            }
        }

        void CollectPrefabReferencesFromFields(object obj)
        {
            if (obj == null)
                return;

            if (TryCollectPrefabReference(obj))
                return;

            const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            var fields = obj.GetType().GetFields(bindingFlags);

            foreach (var field in fields)
            {
                if (field.GetCustomAttributes(typeof(NonSerializedAttribute), false).Length > 0)
                    continue;

                if (!field.IsPublic)
                {
                    if (field.GetCustomAttributes(typeof(SerializeField), false).Length == 0)
                        continue;
                }

                if (field.FieldType == typeof(GameObject))
                {
                    GameObject gameObjectValue = field.GetValue(obj) as GameObject;
                    TryCollectPrefabReference((object)gameObjectValue);
                }

                if (field.FieldType.Name == "AssetReference")
                {
                    var objValue = field.GetValue(obj);
                    TryCollectPrefabReference(objValue);
                }

                if (field.FieldType.IsSubclassOf(typeof(ScriptableObject)))
                {
                    ScriptableObject soValue = field.GetValue(obj) as ScriptableObject;
                    CollectPrefabReferencesFromFields(soValue);
                }

                if (typeof(IEnumerable).IsAssignableFrom(field.FieldType)
                    && !(field.FieldType == typeof(Transform))
                    && !(field.FieldType.IsSubclassOf(typeof(Transform))))
                {
                    var enumerableValue = field.GetValue(obj) as IEnumerable;

                    foreach (var element in enumerableValue)
                    {
                        CollectPrefabReferencesFromFields(element);
                    }
                }
            }
        }

        bool TryCollectPrefabReference(object obj)
        {
            foreach (var collector in m_PrefabCollectors)
            {
                if (collector.TryCollectPrefabReference(obj, m_Prefabs))
                    return true;
            }

            return false;
        }

        void CollectMonoBehavioursFromPrefabRecursive(GameObject go)
        {
            CollectMonoBehaviours(go);

            foreach (Transform childTransform in go.transform)
            {
                CollectMonoBehavioursFromPrefabRecursive(childTransform.gameObject);
            }
        }

        void CollectMonoBehaviours(GameObject go)
        {
            var monoBehaviours = go.GetComponents<MonoBehaviour>();
            foreach (var m in monoBehaviours)
            {
                var type = m.GetType();
                var fullName = type.FullName;

                if (!m_MonoBehaviourCounts.ContainsKey(fullName))
                {
                    m_MonoBehaviourCounts.Add(fullName, 1);
                }
                else
                {
                    m_MonoBehaviourCounts[fullName]++;
                }
            }
        }

        public void Merge(ScenePrefabMonoBehaviourCollector other)
        {
            m_MonoBehaviourCounts = other.m_MonoBehaviourCounts.Concat(m_MonoBehaviourCounts)
                .GroupBy(d => d.Key, (key, counts) => new { Key = key, Value = counts.Sum(d => d.Value) })
                .ToDictionary(d => d.Key, d => d.Value);
        }
    }
}
