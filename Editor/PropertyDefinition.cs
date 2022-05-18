using System;

namespace Unity.ProjectAuditor.Editor
{
    public enum PropertyType
    {
        Description = 0,
        Descriptor,
        Severity,
        Area,
        Path,
        Directory,
        Filename,
        FileType,
        CriticalContext,
        Num
    }

    public struct PropertyTypeUtil
    {
        public static PropertyType FromCustom<T>(T customPropEnum) where T : struct
        {
            return PropertyType.Num + Convert.ToInt32(customPropEnum);
        }
    }

    public enum PropertyFormat
    {
        String = 0,
        Bool,
        Integer,
        Bytes,
        Time
    }

    public struct PropertyDefinition
    {
        public PropertyType type;
        public PropertyFormat format;
        public string name;
        public string longName;
        public bool defaultGroup;
    }

    public class IssueLayout
    {
        public IssueCategory category;
        public PropertyDefinition[] properties;
        public bool hierarchy = false;
    }
}
