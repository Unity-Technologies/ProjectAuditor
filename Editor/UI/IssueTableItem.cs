using UnityEditor.IMGUI.Controls;

namespace Editor
{
    public class IssueTableItem : TreeViewItem
    {
        public ProjectIssue m_projectIssue;
        public IssueTableItem(int id, int depth, string displayName, ProjectIssue projectIssue) : base(id, depth, displayName)
        {
            m_projectIssue = projectIssue;
        }
    }
}