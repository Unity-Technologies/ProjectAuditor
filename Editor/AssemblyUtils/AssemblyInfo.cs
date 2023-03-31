using System;
using System.IO;

namespace Unity.ProjectAuditor.Editor.AssemblyUtils
{
    class AssemblyInfo
    {
        internal const string DefaultAssemblyFileName = "Assembly-CSharp.dll";
        internal static string DefaultAssemblyName => Path.GetFileNameWithoutExtension(DefaultAssemblyFileName);

        internal string name;            // assembly name without extension
        internal string path;            // absolute path
        internal string asmDefPath;
        internal string relativePath;

        internal bool packageReadOnly;
        internal string packageResolvedPath;
    }
}
