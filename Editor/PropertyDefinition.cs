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

    public struct IssueLayout
    {
        public IssueCategory category;
        public PropertyDefinition[] properties;
    }
}
