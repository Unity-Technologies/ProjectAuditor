using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor
{
    public class PlayModeMonoBehaviourCollector : ScriptableSingleton<PlayModeMonoBehaviourCollector>, ISerializationCallbackReceiver
    {
        public struct MonoBehaviourEntry
        {
            public string Name;
            public int Count;
        }

        [SerializeField]
        public List<MonoBehaviourEntry> m_MonoBehaviours = new List<MonoBehaviourEntry>();

        [NonSerialized]
        static public Dictionary<string, int> m_MonoBehaviourMap = new Dictionary<string, int>();

        public void OnBeforeSerialize()
        {
            m_MonoBehaviours.Clear();

            foreach (var e in m_MonoBehaviourMap)
            {
                m_MonoBehaviours.Add(new MonoBehaviourEntry { Name = e.Key, Count = e.Value });
            }
        }

        public void OnAfterDeserialize()
        {
            m_MonoBehaviourMap.Clear();

            foreach (var e in m_MonoBehaviours)
            {
                m_MonoBehaviourMap.Add(e.Name, e.Count);
            }
        }

        public void Collect()
        {
            m_MonoBehaviourMap.Clear();

            var mbs = FindObjectsOfType<MonoBehaviour>();

            foreach (var m in mbs)
            {
                var mbName = m.GetType().FullName;
                if (!m_MonoBehaviourMap.ContainsKey(mbName))
                {
                    m_MonoBehaviourMap.Add(mbName, 1);
                }
                else
                {
                    m_MonoBehaviourMap[mbName]++;
                }
            }
        }
    }

    static class PlayModeSceneCollectorMenus
    {
        [MenuItem("Project Auditor/Capture MonoBehaviours")]
        static void CaptureMonoBehaviours()
        {
            PlayModeMonoBehaviourCollector.instance.Collect();
        }
    }
}
