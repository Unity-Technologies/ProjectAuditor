using System.Collections.Generic;
using System.Linq;
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

            if (string.IsNullOrEmpty(id))
            {
                descriptor = null;
                return false;
            }

            return m_Descriptors.TryGetValue(id, out descriptor);
        }

        public static void AddDescriptors(List<Descriptor> descriptors)
        {
            if (m_Descriptors == null)
            {
                m_Descriptors = new Dictionary<string, Descriptor>();
            }

            foreach (var descriptor in descriptors)
            {
                RegisterDescriptor(descriptor.id, descriptor);
            }
        }
    }
}
