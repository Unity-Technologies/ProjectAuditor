using System;
using UnityEditor;

namespace Unity.ProjectAuditor.Editor.Core
{
    internal class AnalysisPlatformAttribute : Attribute
    {
        public BuildTarget Platform { get;}

        public AnalysisPlatformAttribute(BuildTarget platform)
        {
            this.Platform = platform;
        }
    }
}
