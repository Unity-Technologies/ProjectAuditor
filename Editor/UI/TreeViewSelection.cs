using System.Collections.Generic;

// SteveM TODO - Wanted to make this a generalised tree view selection (because I'm going to use it for both Assemblies and Areas),
// but right now it relies on AssemblyIdentifier. Maybe that can be made generic too? Or this can become some kind of generic base class?
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

        public void SetAll()
        {
            groups.Clear();
            selection.Clear();

            AssemblyIdentifier allTreeViewSelection = new AssemblyIdentifier("All",AssemblyIdentifier.kAll);
            groups.Add(allTreeViewSelection.assemblyNameWithIndex);
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

            AssemblyIdentifier allTreeViewSelection = new AssemblyIdentifier(groupName,AssemblyIdentifier.kAll);
            groups.Add(allTreeViewSelection.assemblyNameWithIndex);
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
