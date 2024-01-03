using System;
using System.Linq;
using UnityEditor.IMGUI.Controls;

namespace Unity.ProjectAuditor.Editor.UI.Framework
{
    class IssueTableItem : TreeViewItem
    {
        public readonly string GroupName;
        public readonly ProjectIssue ProjectIssue;

        public int NumVisibleChildren;

        public virtual string DisplayName
        {
            get => displayName;
            set => displayName = value;
        }

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
            return ProjectIssue.Description;
        }

        public bool Find(ProjectIssue issue)
        {
            if (ProjectIssue == issue)
                return true;
            return children != null && children.FirstOrDefault(child => (child as IssueTableItem).ProjectIssue == issue) != null;
        }
    }
}
