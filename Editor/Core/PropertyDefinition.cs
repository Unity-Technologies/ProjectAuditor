using System;

namespace Unity.ProjectAuditor.Editor.Core
{
    internal enum PropertyType
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

    internal struct PropertyTypeUtil
    {
        internal static PropertyType FromCustom<T>(T customPropEnum) where T : struct
        {
            return PropertyType.Num + Convert.ToInt32(customPropEnum);
        }

        internal static int ToCustomIndex(PropertyType type)
        {
            return type - PropertyType.Num;
        }

        internal static bool IsCustom(PropertyType type)
        {
            return type >= PropertyType.Num;
        }
    }

    internal enum PropertyFormat
    {
        String = 0,
        Bool,
        Integer,
        ULong,
        Bytes,
        Time,
        Percentage
    }

    internal struct PropertyDefinition
    {
        internal PropertyType type;
        internal PropertyFormat format;
        internal string name;
        internal string longName;
        internal bool defaultGroup;
        internal bool hidden;
    }
}
