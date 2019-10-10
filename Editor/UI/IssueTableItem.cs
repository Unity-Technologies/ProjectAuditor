using UnityEditor.IMGUI.Controls;

namespace Unity.ProjectAuditor.Editor
{
    public class IssueTableItem : TreeViewItem
    {
        public ProjectIssue m_ProjectIssue;
        public ProblemDefinition m_ProblemDefinition;

        public IssueTableItem(int id, int depth, string displayName, ProblemDefinition problemDefinition, ProjectIssue projectIssue = null) : base(id, depth, displayName)
        {
            m_ProblemDefinition = problemDefinition;
            m_ProjectIssue = projectIssue;
        }
    }
}