using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.ProjectAuditor.Editor.MonoBehaviourCollectors
{
    public class SceneMonoBehaviourCollector
    {
        Dictionary<string, int> m_MonoBehaviourCounts = new Dictionary<string, int>();

        public Dictionary<string, int> MonoBehaviourCounts
        {
            get { return m_MonoBehaviourCounts; }
        }

        public void Collect(Scene scene)
        {
            var rootGameObjects = scene.GetRootGameObjects();
            foreach (var go in rootGameObjects)
            {
                CollectRecursive(go);
            }
        }

        public void CollectRecursive(GameObject go)
        {
            CollectMonoBehaviours(go);

            foreach (Transform childTransform in go.transform)
            {
                CollectRecursive(childTransform.gameObject);
            }
        }

        public void CollectMonoBehaviours(GameObject go)
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

        public void Merge(SceneMonoBehaviourCollector other)
        {
            m_MonoBehaviourCounts = other.m_MonoBehaviourCounts.Concat(m_MonoBehaviourCounts)
                .GroupBy(d => d.Key, (key, counts) => new { Key = key, Value = counts.Sum(d => d.Value) })
                .ToDictionary(d => d.Key, d => d.Value);
        }
    }
}
