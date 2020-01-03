using System.Collections.Generic;
using UnityEngine;

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

        public TreeViewSelection(TreeViewSelection threadSelection)
        {
            groups = new List<string>();
            selection = new List<string>();

            Set(threadSelection);
        }

        public void SetAll(string[] names)
        {
            groups.Clear();
             TreeItemIdentifier allIdentifier = new TreeItemIdentifier("All",TreeItemIdentifier.kAll);
            groups.Add(allIdentifier.nameWithIndex);
            
            selection.Clear();
            foreach (string nameWithIndex in names)
            {
                if (nameWithIndex != allIdentifier.nameWithIndex)
                {
                    var identifier = new TreeItemIdentifier(nameWithIndex);
                    if (identifier.index != TreeItemIdentifier.kAll)
                    {
                        selection.Add(nameWithIndex);
                    }
                }
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

            TreeItemIdentifier allTreeViewSelection = new TreeItemIdentifier(groupName,TreeItemIdentifier.kAll);
            groups.Add(allTreeViewSelection.nameWithIndex);
        }

        public void Set(TreeViewSelection threadSelection)
        {
            groups.Clear();
            selection.Clear();

            if (threadSelection.groups != null)
                groups.AddRange(threadSelection.groups);
            if (threadSelection.selection != null)
                selection.AddRange(threadSelection.selection);
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
