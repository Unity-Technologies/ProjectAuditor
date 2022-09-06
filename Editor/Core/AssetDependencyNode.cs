using System;

namespace Unity.ProjectAuditor.Editor.Core
{
    public class AssetDependencyNode : DependencyNode
    {
        public override string GetName()
        {
            return location.Filename;
        }

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
