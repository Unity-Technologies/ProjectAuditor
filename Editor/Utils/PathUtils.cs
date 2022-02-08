using System;
using File = System.IO.File;
using SystemPath = System.IO.Path;

namespace Unity.ProjectAuditor.Editor.Utils
{
    internal class PathUtils
    {
        public static readonly char Separator = '/';

        public static string Combine(string[] parts)
        {
            return string.Join(Char.ToString(Separator), parts);
        }

        public static string Combine(string path1, string path2)
        {
            return ReplaceSeparators(SystemPath.Combine(path1, path2));
        }

        public static string GetDirectoryName(string path)
        {
            return ReplaceSeparators(SystemPath.GetDirectoryName(path));
        }

        public static string GetFullPath(string path)
        {
            return ReplaceSeparators(SystemPath.GetFullPath(path));
        }

        public static string ReplaceSeparators(string path)
        {
            int length = path.Length;

            var chars = new char[length];

            for (int i = 0; i < length; ++i)
            {
                if (path[i] == '\\')
                    chars[i] = Separator;
                else
                    chars[i] = path[i];
            }

            return new string(chars);
        }

        public static bool Exists(string path)
        {
            return File.Exists(path);
        }

        public static string[] Split(string path)
        {
            return path.Split(Separator);
        }
    }
}
