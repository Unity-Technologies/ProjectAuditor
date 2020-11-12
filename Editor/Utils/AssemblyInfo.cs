using System;

namespace Unity.ProjectAuditor.Editor.Utils
{
    class AssemblyInfo
    {
        public string name;            // assembly name without extension
        public string path;            // absolute path
        public string asmDefPath;
        public string relativePath;
        public bool readOnly;
        public string[] sourcePaths;
    }
}
