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

        public static int ToCustomIndex(PropertyType type)
        {
            return type - PropertyType.Num;
        }

        public static bool IsCustom(PropertyType type)
        {
            return type >= PropertyType.Num;
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
