using System;
using System.Collections.Generic;
using System.IO;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEngine.Serialization;

namespace Unity.ProjectAuditor.Editor.Core
{
    internal static class RoslynTextLookup
    {
        [Serializable]
        struct RawStringLookup
        {
#pragma warning disable 649 // Disable warning CS0649. The fields are assigned through Newtonsoft.Json
            public string id;
            public string description;
            public string recommendation;
#pragma warning restore 649
        }

        struct StringLookup
        {
            public string description;
            public string recommendation;

            public StringLookup(string description, string recommendation)
            {
                this.description = description;
                this.recommendation = recommendation;
            }
        }

        private static Dictionary<string, StringLookup> m_StringLookup;

        public static void Initialize()
        {
            m_StringLookup = new Dictionary<string, StringLookup>();

            var rawDescriptors =
                Json.DeserializeArrayFromFile<RawStringLookup>(Path.Combine(ProjectAuditor.s_DataPath, "RoslynTextLookup.json"));

            foreach (var rawDescriptor in rawDescriptors)
            {
                m_StringLookup[rawDescriptor.id] = new StringLookup(rawDescriptor.description, rawDescriptor.recommendation);
            }
        }

        public static string GetDescription(string id)
        {
            if (m_StringLookup == null)
                Initialize();

            return m_StringLookup[id].description;
        }

        public static string GetRecommendation(string id)
        {
            if (m_StringLookup == null)
                Initialize();

            return m_StringLookup[id].recommendation;
        }
    }
}
