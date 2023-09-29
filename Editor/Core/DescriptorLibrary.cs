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
                // Don't overwrite existing descriptor data from a saved report: an updated package may have specified
                // newer versions of this data which we'll want to keep.
                if (!m_Descriptors.ContainsKey(DescriptorID.HashDescriptorString(descriptor.id)))
                {
                    RegisterDescriptor(descriptor.id, descriptor);
                }
            }
        }

        public void OnBeforeSerialize()
        {
            // update list from dictionary
            // TODO: Serialization is needed to survive domain reload, and when writing a ProjectReport out to file.
            // In both cases the list only really needs to contain the Descriptors that correspond to ProjectIssues
            // actually found in the report, so if we had the report object we could potentially do some filtering here.
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
