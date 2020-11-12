using System;
using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Utils;

namespace Unity.ProjectAuditor.Editor
{
    public abstract class DependencyNode
    {
        protected List<DependencyNode> m_Children = new List<DependencyNode>();

        public Location location;
        public bool perfCriticalContext;

        public string prettyName
        {
            get { return GetPrettyName(); }
        }

        public bool HasValidChildren()
        {
            return m_Children != null;
        }

        public bool HasChildren()
        {
            return m_Children != null && m_Children.Count > 0;
        }

        public void AddChild(DependencyNode child)
        {
            m_Children.Add(child);
        }

        public DependencyNode GetChild(int index = 0)
        {
            return m_Children[index];
        }

        public int GetNumChildren()
        {
            return m_Children.Count;
        }

        public abstract string GetPrettyName();
        public abstract bool IsPerfCritical();
    }
}
