using System.Collections.Generic;
using System.Linq;
using Unity.ProjectAuditor.Editor.Diagnostic;

namespace Unity.ProjectAuditor.Editor.Diagnostic
{
    internal static class DescriptorLibrary
    {
        static Dictionary<int, Descriptor> m_Descriptors;

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
    }
}
