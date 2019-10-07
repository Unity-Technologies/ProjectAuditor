using UnityEditor.IMGUI.Controls;

namespace Unity.ProjectAuditor.Editor
{
    public class IssueTableItem : TreeViewItem
    {
        public ProjectIssue m_projectIssue;
        public IssueTableItem(int id, int depth, string displayName, ProjectIssue projectIssue = null) : base(id, depth, displayName)
        {
            m_projectIssue = projectIssue;
        }
    }
}