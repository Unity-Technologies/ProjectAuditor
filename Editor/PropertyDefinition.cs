using System;

namespace Unity.ProjectAuditor.Editor
{
    public enum PropertyType
    {
        Description = 0,
        Severity,
        Area,
        Path,
        Filename,
        FileType,
        CriticalContext,
        Custom
    }

    public enum PropertyFormat
    {
        Bool = 0,
        Integer,
        String
    }

    public struct PropertyDefinition
    {
        public PropertyType type;
        public PropertyFormat format;
        public string name;
        public string longName;
    }

    public class IssueLayout
    {
        public IssueCategory category;
        public PropertyDefinition[] properties;
    }
}
