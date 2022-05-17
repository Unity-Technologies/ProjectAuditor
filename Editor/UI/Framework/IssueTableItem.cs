using System;
using System.Linq;
using UnityEditor.IMGUI.Controls;

namespace Unity.ProjectAuditor.Editor.UI.Framework
{
    public class IssueTableItem : TreeViewItem
    {
        public readonly string GroupName;
        public readonly ProjectIssue ProjectIssue;

        public IssueTableItem(int id, int depth, string displayName,
                              ProjectIssue projectIssue, string groupName = null) : base(id, depth, displayName)
        {
            GroupName = groupName;
            ProjectIssue = projectIssue;
        }

        public IssueTableItem(int id, int depth, string groupName) : base(id, depth)
        {
            GroupName = groupName;
        }

        public bool IsGroup()
        {
            return (ProjectIssue == null);
        }

        public string GetDisplayName()
        {
            if (IsGroup())
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
