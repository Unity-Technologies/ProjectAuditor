using System;
using System.Linq;
using UnityEditor.IMGUI.Controls;

namespace Unity.ProjectAuditor.Editor.UI.Framework
{
    class IssueTableItem : TreeViewItem
    {
        internal readonly string GroupName;
        internal readonly ProjectIssue ProjectIssue;

        internal IssueTableItem(int id, int depth, string displayName,
                              ProjectIssue projectIssue, string groupName = null) : base(id, depth, displayName)
        {
            GroupName = groupName;
            ProjectIssue = projectIssue;
        }

        internal IssueTableItem(int id, int depth, string groupName) : base(id, depth)
        {
            GroupName = groupName;
        }

        internal bool IsGroup()
        {
            return (ProjectIssue == null);
        }

        internal string GetDisplayName()
        {
            if (IsGroup())
                return displayName;
            return ProjectIssue.description;
        }

        internal bool Find(ProjectIssue issue)
        {
            if (ProjectIssue == issue)
                return true;
            return children != null && children.FirstOrDefault(child => (child as IssueTableItem).ProjectIssue == issue) != null;
        }
    }
}
