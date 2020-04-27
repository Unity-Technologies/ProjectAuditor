using UnityEditor.IMGUI.Controls;

namespace Unity.ProjectAuditor.Editor.UI
{
    internal class IssueTableItem : TreeViewItem
    {
        public readonly ProblemDescriptor ProblemDescriptor;
        public readonly ProjectIssue ProjectIssue;

        public IssueTableItem(int id, int depth, string displayName, ProblemDescriptor problemDescriptor,
                              ProjectIssue projectIssue) : base(id, depth, displayName)
        {
            ProblemDescriptor = problemDescriptor;
            ProjectIssue = projectIssue;
        }

        public IssueTableItem(int id, int depth, ProblemDescriptor problemDescriptor) : base(id, depth)
        {
            ProblemDescriptor = problemDescriptor;
        }
    }
}
