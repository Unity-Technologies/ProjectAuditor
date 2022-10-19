using System;
using System.IO;

namespace Unity.ProjectAuditor.Editor.AssemblyUtils
{
    class AssemblyInfo
    {
        public const string DefaultAssemblyFileName = "Assembly-CSharp.dll";
        public static string DefaultAssemblyName => Path.GetFileNameWithoutExtension(DefaultAssemblyFileName);

        public string name;            // assembly name without extension
        public string path;            // absolute path
        public string asmDefPath;
        public string relativePath;

        public bool packageReadOnly;
        public string packageResolvedPath;
    }
}
