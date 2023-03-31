using System;

namespace Unity.ProjectAuditor.Editor.Core
{
    internal class IssueLayout
    {
        internal IssueCategory category;
        internal PropertyDefinition[] properties;
        internal bool hierarchy = false;

        internal int defaultGroupPropertyIndex
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
