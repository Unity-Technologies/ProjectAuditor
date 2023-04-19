using System;

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
    }
}
