using System;
using System.Linq;
using UnityEditor.IMGUI.Controls;

namespace Unity.ProjectAuditor.Editor.UI.Framework
{
    class IssueTableItem : TreeViewItem
    {
        public readonly string GroupName;
        public readonly ReportItem ReportItem;

        public int NumVisibleChildren;

        public virtual string DisplayName
        {
            get => displayName;
            set => displayName = value;
        }

        public IssueTableItem(int id, int depth, string displayName,
                              ReportItem reportItem, string groupName = null) : base(id, depth, displayName)
        {
            GroupName = groupName;
            ReportItem = reportItem;
        }

        public IssueTableItem(int id, int depth, string groupName) : base(id, depth)
        {
            GroupName = groupName;
        }

        public bool IsGroup()
        {
            return (ReportItem == null);
        }

        public string GetDisplayName()
        {
            if (IsGroup())
                return displayName;
            return ReportItem.Description;
        }

        public bool Find(ReportItem issue)
        {
            if (ReportItem == issue)
                return true;
            return children != null && children.FirstOrDefault(child => (child as IssueTableItem).ReportItem == issue) != null;
        }
    }
}
