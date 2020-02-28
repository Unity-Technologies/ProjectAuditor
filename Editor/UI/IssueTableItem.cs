using UnityEditor.IMGUI.Controls;

namespace Unity.ProjectAuditor.Editor
{
    internal class IssueTableItem : TreeViewItem
    {
        public readonly ProblemDescriptor ProblemDescriptor;
        public readonly ProjectIssue ProjectIssue;

        public IssueTableItem(int id, int depth, string displayName, ProblemDescriptor problemDescriptor,
            ProjectIssue projectIssue = null) : base(id, depth, displayName)
        {
            ProblemDescriptor = problemDescriptor;
            ProjectIssue = projectIssue;
        }
    }
}