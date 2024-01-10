namespace Unity.ProjectAuditor.Editor.Core
{
    internal class SimpleDependencyNode : DependencyNode
    {
        readonly string m_Name;

        public SimpleDependencyNode(string name)
        {
            m_Name = name;
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
