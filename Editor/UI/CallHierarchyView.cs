using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;


namespace Unity.ProjectAuditor.Editor
{
	class CallHierarchyView : TreeView
	{
		private MethodInstance m_CallTree = null;
		
		public CallHierarchyView(TreeViewState treeViewState)
			: base(treeViewState)
		{
			Reload();
		}
		
		protected override TreeViewItem BuildRoot ()
		{
			var root = new TreeViewItem {id = 0, depth = -1, displayName = "Hidden Root"};
			var allItems = new List<TreeViewItem>();

			if (m_CallTree != null)
				BuildNode(allItems, m_CallTree, 1);
										  
			// Utility method that initializes the TreeViewItem.children and -parent for all items.
			SetupParentsAndChildrenFromDepths (root, allItems);
			
			// Return root of the tree
			return root;
		}

		public void SetCallTree(MethodInstance callTree)
		{
			m_CallTree = callTree;
		}

		void BuildNode(List<TreeViewItem> items, MethodInstance method, int depth)
		{
			int id = items.Count;
			items.Add(new TreeViewItem {id = id, depth = depth, displayName = method.name.Substring(method.name.IndexOf(" "))});
			
            foreach (var parent in method.parents)
            {
	            BuildNode(items, parent, depth + 1);
            }
		}
			
	}
}

