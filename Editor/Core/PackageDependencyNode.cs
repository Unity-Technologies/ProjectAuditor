namespace Unity.ProjectAuditor.Editor.Core
{
    public class PackageDependencyNode : DependencyNode
    {
        readonly string m_Name;

        public PackageDependencyNode(string name, string[] deps = null)
        {
            m_Name = name;
            if (deps != null)
            {
                for (int i = 0; i < deps.Length; i++)
                {
                    AddChild(new PackageDependencyNode(deps[i]));
                }
            }
        }

        public override string GetName()
        {
            return m_Name;
        }

        public override string GetPrettyName()
        {
            return m_Name;
        }

        public override bool IsPerfCritical()
        {
            return false;
        }
    }
}
