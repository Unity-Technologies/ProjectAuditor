using System;

namespace Unity.ProjectAuditor.Editor.Core
{
    public enum PropertyType
    {
        Description = 0,
        Descriptor,
        Severity,
        LogLevel,
        Area,
        Path,
        Directory,
        Filename,
        FileType,
        Platform,
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
        public bool hidden;
    }
}
