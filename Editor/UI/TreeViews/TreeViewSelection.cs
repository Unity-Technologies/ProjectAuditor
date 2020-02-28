using System.Collections.Generic;

namespace Unity.ProjectAuditor.Editor
{
    public class TreeViewSelection
    {
        public List<string> groups;
        public List<string> selection;

        public TreeViewSelection()
        {
            groups = new List<string>();
            selection = new List<string>();
        }

        public TreeViewSelection(TreeViewSelection selection)
        {
            groups = new List<string>();
            this.selection = new List<string>();

            Set(selection);
        }

        public void SetAll(string[] names)
        {
            groups.Clear();
            var allIdentifier = new TreeItemIdentifier("All", TreeItemIdentifier.kAll);
            groups.Add(allIdentifier.nameWithIndex);

            selection.Clear();
            foreach (var nameWithIndex in names)
                if (nameWithIndex != allIdentifier.nameWithIndex)
                {
                    var identifier = new TreeItemIdentifier(nameWithIndex);
                    if (identifier.index != TreeItemIdentifier.kAll) selection.Add(nameWithIndex);
                }
        }

        public void Set(string name)
        {
            groups.Clear();
            selection.Clear();
            selection.Add(name);
        }

        public void SetGroup(string groupName)
        {
            groups.Clear();
            selection.Clear();

            var allTreeViewSelection = new TreeItemIdentifier(groupName, TreeItemIdentifier.kAll);
            groups.Add(allTreeViewSelection.nameWithIndex);
        }

        public void Set(TreeViewSelection selection)
        {
            groups.Clear();
            this.selection.Clear();

            if (selection.groups != null)
                groups.AddRange(selection.groups);
            if (selection.selection != null)
                this.selection.AddRange(selection.selection);
        }

        public bool Contains(string name)
        {
            return selection.Contains(name);
        }

        public bool ContainsGroup(string groupName)
        {
            return groups.Contains(groupName);
        }
    }
}