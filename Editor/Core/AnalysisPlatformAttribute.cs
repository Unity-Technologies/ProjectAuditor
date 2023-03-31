using System;
using UnityEditor;

namespace Unity.ProjectAuditor.Editor.Core
{
    internal class AnalysisPlatformAttribute : Attribute
    {
        internal BuildTarget platform { get;}

        internal AnalysisPlatformAttribute(BuildTarget platform)
        {
            this.platform = platform;
        }
    }
}
