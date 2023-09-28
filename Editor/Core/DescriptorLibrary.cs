using System;
using System.Collections.Generic;
using System.Linq;
using Unity.ProjectAuditor.Editor.Diagnostic;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Diagnostic
{
    [Serializable]
    public class DescriptorLibrary : ISerializationCallbackReceiver
    {
        static Dictionary<int, Descriptor> m_Descriptors;

        [SerializeField]
        List<Descriptor> m_SerializedDescriptors;

        public static bool RegisterDescriptor(string id, Descriptor descriptor)
        {
            return RegisterDescriptor(new DescriptorID(id), descriptor);
        }

        public static bool RegisterDescriptor(DescriptorID id, Descriptor descriptor)
        {
            if(m_Descriptors == null)
                m_Descriptors = new Dictionary<int, Descriptor>();

            bool alreadyFound = m_Descriptors.ContainsKey(id);
            m_Descriptors[id] = descriptor;
            return alreadyFound;
        }

        public static Descriptor GetDescriptor(int idAsInt)
        {
            return m_Descriptors[idAsInt];
        }

        public static void AddDescriptors(List<Descriptor> descriptors)
        {
            if (m_Descriptors == null)
            {
                m_Descriptors = new Dictionary<int, Descriptor>();
            }

            foreach (var descriptor in descriptors)
            {
                RegisterDescriptor(descriptor.id, descriptor);
            }
        }

        public void OnBeforeSerialize()
        {
            // update list from dictionary
            m_SerializedDescriptors = m_Descriptors.Values.ToList();
        }

        public void OnAfterDeserialize()
        {
            // update dictionary from list
            m_Descriptors = m_SerializedDescriptors.ToDictionary(m => new DescriptorID(m.id).AsInt(), m => m);
            m_SerializedDescriptors = null;
        }
    }
}
