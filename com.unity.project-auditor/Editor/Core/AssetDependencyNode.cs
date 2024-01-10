using System;

namespace Unity.ProjectAuditor.Editor.Core
{
    internal class AssetDependencyNode : DependencyNode
    {
        public override string GetName()
        {
            return Location.Filename;
        }

        public override string GetPrettyName()
        {
            return Location.Path;
        }

        public override bool IsPerfCritical()
        {
            return false;
        }
    }
}
