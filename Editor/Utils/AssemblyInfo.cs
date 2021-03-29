using System;
using System.IO;

namespace Unity.ProjectAuditor.Editor.Utils
{
    class AssemblyInfo
    {
        public const string DefaultAssemblyFileName = "Assembly-CSharp.dll";
        public static string DefaultAssemblyName
        {
            get { return Path.GetFileNameWithoutExtension(DefaultAssemblyFileName); }
        }

        public string name;            // assembly name without extension
        public string path;            // absolute path
        public string asmDefPath;
        public string relativePath;
        public bool readOnly;
        public string[] sourcePaths;
    }
}
