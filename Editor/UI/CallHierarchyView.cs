using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;


namespace Unity.ProjectAuditor.Editor
{
	class CallHierarchyView : TreeView
	{
		private CallInstance m_RootCall = null;
		
		public CallHierarchyView(TreeViewState treeViewState)
			: base(treeViewState)
		{
			Reload();
		}
		
		protected override TreeViewItem BuildRoot ()
		{
			var root = new TreeViewItem {id = 0, depth = -1, displayName = "Hidden Root"};
			var allItems = new List<TreeViewItem>();

			if (m_RootCall != null)
				BuildNode(allItems, m_RootCall, 1);
										  
			// Utility method that initializes the TreeViewItem.children and -parent for all items.
			SetupParentsAndChildrenFromDepths (root, allItems);
			
			// Return root of the tree
			return root;
		}

		public void SetCallHierarchy(CallInstance rootCall)
		{
			m_RootCall = rootCall;
		}

		void BuildNode(List<TreeViewItem> items, CallInstance callTree, int depth)
		{
			int id = items.Count;
			items.Add(new TreeViewItem {id = id, depth = depth, displayName = callTree.name.Substring(callTree.name.IndexOf(" "))});
			
            foreach (var parent in callTree.children)
            {
	            BuildNode(items, parent, depth + 1);
            }
		}
	}
}

