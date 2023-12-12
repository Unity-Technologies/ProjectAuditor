using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Unity.ProjectAuditor.Editor.Core
{
    internal enum PropertyType
    {
        Description = 0,
        Descriptor,
        Severity,
        LogLevel,
        Areas,
        Path,
        Directory,
        Filename,
        FileType,
        Platform,
        Num
    }

    internal struct PropertyTypeUtil
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

    [JsonConverter(typeof(StringEnumConverter))]
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
        [JsonIgnore]
        public PropertyType Type;
        public PropertyFormat Format;
        public string Name;
        public string LongName;
        public int MaxAutoWidth;
        public bool IsDefaultGroup;
        public bool IsHidden;
    }
}
