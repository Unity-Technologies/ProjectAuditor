using System;
using UnityEditor;

namespace Unity.ProjectAuditor.Editor.Core
{
    internal class AnalysisPlatformAttribute : Attribute
    {
        public BuildTarget platform { get;}

        public AnalysisPlatformAttribute(BuildTarget platform)
        {
            this.platform = platform;
        }
    }
}
