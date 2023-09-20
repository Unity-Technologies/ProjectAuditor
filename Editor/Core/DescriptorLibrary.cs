using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Diagnostic;

namespace Unity.ProjectAuditor.Editor.Core
{
    internal static class DescriptorLibrary
    {
        static Dictionary<string, Descriptor> m_Descriptors;

        public static bool RegisterDescriptor(string id, Descriptor descriptor)
        {
            if(m_Descriptors == null)
                m_Descriptors = new Dictionary<string, Descriptor>();

            bool alreadyFound = m_Descriptors.ContainsKey(id);
            m_Descriptors[id] = descriptor;
            return alreadyFound;
        }

        public static bool TryGetDescriptor(string id, out Descriptor descriptor)
        {
            if (m_Descriptors == null)
            {
                m_Descriptors = new Dictionary<string, Descriptor>();
                descriptor = null;
                return false;
            }

            return m_Descriptors.TryGetValue(id, out descriptor);
        }

        public static Descriptor GetDescriptor(string id)
        {
            if (m_Descriptors == null)
            {
                m_Descriptors = new Dictionary<string, Descriptor>();
            }

            if (string.IsNullOrEmpty(id) || !m_Descriptors.ContainsKey(id))
            {
                return new Descriptor("PAX0000", "NULL DESCRIPTOR", Area.CPU, "This descriptor is not meant to be used. Something has gone wrong", "Contact Unity to report a bug");
            }

            return m_Descriptors[id];
        }

        public static bool IsDescriptorRegistered(string id)
        {
            if (m_Descriptors == null)
                return false;

            return m_Descriptors.ContainsKey(id);
        }
    }
}
