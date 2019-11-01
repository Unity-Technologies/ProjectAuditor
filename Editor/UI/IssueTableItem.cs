using UnityEditor.IMGUI.Controls;

namespace Unity.ProjectAuditor.Editor
{
    public class IssueTableItem : TreeViewItem
    {
        public ProjectIssue m_ProjectIssue;
        public ProblemDescriptor problemDescriptor;

        public IssueTableItem(int id, int depth, string displayName, ProblemDescriptor problemDescriptor, ProjectIssue projectIssue = null) : base(id, depth, displayName)
        {
            this.problemDescriptor = problemDescriptor;
            m_ProjectIssue = projectIssue;
        }
    }
}