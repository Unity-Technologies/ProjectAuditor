using System;
using UnityEditor;
using Unity.ProjectAuditor.Editor.Utils; // Required for TypeCache in Unity 2018

namespace Unity.ProjectAuditor.Editor.Core
{
    internal class IssueLayout
    {
        public IssueCategory category;
        public PropertyDefinition[] properties;
        public bool hierarchy = false;

        public int defaultGroupPropertyIndex
        {
            get
            {
                if (hierarchy)
                    return -1;
                return Array.FindIndex(properties, p => p.defaultGroup);
            }
        }

        /// <summary>
        /// Get the layout for a category
        /// </summary>
        /// <param name="category">The category to get the layout for</param>
        /// <returns>The IssueLayout for the specified category</returns>
        public static IssueLayout GetLayout(IssueCategory category)
        {
            if (category == IssueCategory.Metadata)
                return new IssueLayout {category = IssueCategory.Metadata, properties = new PropertyDefinition[] {}};

            foreach (var type in TypeCache.GetTypesDerivedFrom(typeof(Module)))
            {
                if (type.IsAbstract)
                    continue;
                var module = Activator.CreateInstance(type) as Module;
                if (module.isSupported)
                {
                    foreach (var layout in module.supportedLayouts)
                    {
                        if (layout.category == category)
                            return layout;
                    }
                }
            }
            return null;
        }
    }
}
