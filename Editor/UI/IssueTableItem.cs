using System.Linq;
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

        public string GetDisplayName()
        {
            if (hasChildren)
                return displayName;
            return ProjectIssue.description;
        }

        public bool Find(ProjectIssue issue)
        {
            if (ProjectIssue == issue)
                return true;
            return children != null && children.FirstOrDefault(child => (child as IssueTableItem).ProjectIssue == issue) != null;
        }
    }
}
