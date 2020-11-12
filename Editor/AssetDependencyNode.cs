using System;

namespace Unity.ProjectAuditor.Editor
{
    public class AssetDependencyNode : DependencyNode
    {
        public override string GetPrettyName()
        {
            return location.Path;
        }

        public override bool IsPerfCritical()
        {
            return false;
        }
    }
}
