using System;
using System.Runtime.CompilerServices;
using File = System.IO.File;
using SystemPath = System.IO.Path;

[assembly:InternalsVisibleTo("Unity.ProjectAuditor.Editor.Tests.Common")]
namespace Unity.ProjectAuditor.Editor.Utils
{
    internal static class PathUtils
    {
        internal const char Separator = '/';

        static readonly char k_DirectorySeparatorChar = SystemPath.DirectorySeparatorChar;
        static readonly char k_AltDirectorySeparatorChar = SystemPath.AltDirectorySeparatorChar;
        static readonly char k_VolumeSeparatorChar = SystemPath.VolumeSeparatorChar;

        internal static string Combine(params string[] parts)
        {
            return string.Join(Char.ToString(Separator), parts);
        }

        internal static string Combine(string path1, string path2)
        {
            return ReplaceSeparators(SystemPath.Combine(path1, path2));
        }

        internal static string GetDirectoryName(string path)
        {
            return ReplaceSeparators(SystemPath.GetDirectoryName(path));
        }

        internal static string GetFullPath(string path)
        {
            return ReplaceSeparators(SystemPath.GetFullPath(path));
        }

        internal static int GetExtensionIndexFromPath(string path)
        {
            var length = path.Length;

            if (length == 0)
                return 0;

            var num = length;
            while (--num >= 0)
            {
                var c = path[num];
                if (c == '.')
                {
                    if (num != length - 1)
                    {
                        return num;
                    }

                    return length - 1;
                }

                if (c == k_DirectorySeparatorChar || c == k_AltDirectorySeparatorChar || c == k_VolumeSeparatorChar)
                {
                    return length - 1;
                }
            }
            return length - 1;
        }

        internal static int GetFilenameIndexFromPath(string path)
        {
            var length = path.Length;
            var num = length;
            while (--num >= 0)
            {
                var c = path[num];
                if (c == k_DirectorySeparatorChar || c == k_AltDirectorySeparatorChar || c == k_VolumeSeparatorChar)
                {
                    return num + 1;
                }
            }
            return 0;
        }

        internal static string ReplaceSeparators(string path)
        {
            var length = path.Length;

            var chars = new char[length];

            for (var i = 0; i < length; ++i)
            {
                if (path[i] == '\\')
                    chars[i] = Separator;
                else
                    chars[i] = path[i];
            }

            return new string(chars);
        }

        internal static bool Exists(string path)
        {
            return File.Exists(path);
        }

        internal static string ReplaceInvalidChars(string path)
        {
            return path.Replace('|', '_').Replace(":", string.Empty);
        }

        internal static string[] Split(string path)
        {
            return path.Split(Separator);
        }
    }
}
