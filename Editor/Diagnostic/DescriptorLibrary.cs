using System;
using System.Collections.Generic;
using System.Linq;
using Unity.ProjectAuditor.Editor.Diagnostic;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Diagnostic
{
    [Serializable]
    class DescriptorLibrary : ISerializationCallbackReceiver
    {
        static Dictionary<int, Descriptor> s_Descriptors;

        [SerializeField]
        internal List<Descriptor> m_SerializedDescriptors;

        public static bool RegisterDescriptor(string id, Descriptor descriptor)
        {
            return RegisterDescriptor(new DescriptorId(id), descriptor);
        }

        public static bool RegisterDescriptor(DescriptorId id, Descriptor descriptor)
        {
            if (s_Descriptors == null)
                s_Descriptors = new Dictionary<int, Descriptor>();

            bool alreadyFound = s_Descriptors.ContainsKey(id);
            s_Descriptors[id] = descriptor;
            return alreadyFound;
        }

        public static Descriptor GetDescriptor(int idAsInt)
        {
            return s_Descriptors[idAsInt];
        }

        public void OnBeforeSerialize()
        {
            // update list from dictionary

            // TODO: Serialization is needed to survive domain reload, and when writing a ProjectReport out to file.
            // In both cases the list only really needs to contain the Descriptors that correspond to ProjectIssues
            // actually found in the report, so if we had the report object we could potentially do some filtering here.
            m_SerializedDescriptors = s_Descriptors.Values.ToList();
        }

        public void OnAfterDeserialize()
        {
            // update dictionary from list

            // TODO: _Hypothetically_, if we're here after loading an old report from JSON, and if we're in a newer
            // version of the tool with updated descriptors, we might want to keep those descriptors in the library
            // rather than overwrite them with the ones that were saved alongside the issues. Right now it's such an
            // edge case that it doesn't really seem worth spending time on.
            if (m_SerializedDescriptors != null)
            {
                s_Descriptors = m_SerializedDescriptors.ToDictionary(m => new DescriptorId(m.Id).AsInt(), m => m);
                m_SerializedDescriptors = null;
            }
        }

        // For testing purposes only
        internal static void Reset()
        {
            s_Descriptors?.Clear();
        }
    }
}
