using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;


namespace Unity.ProjectAuditor.Editor
{
	internal class CallHierarchyView : TreeView
	{
		private CallTreeNode m_CallTree = null;
		
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
				BuildNode(allItems, m_CallTree, 0);
										  
			// Utility method that initializes the TreeViewItem.children and -parent for all items.
			SetupParentsAndChildrenFromDepths (root, allItems);
			
			// Return root of the tree
			return root;
		}

		public void SetCallTree(CallTreeNode callTree)
		{
			m_CallTree = callTree;
		}

		void BuildNode(List<TreeViewItem> items, CallTreeNode callTree, int depth)
		{
			int id = items.Count;
			items.Add(new TreeViewItem {id = id, depth = depth, displayName = callTree.prettyName});

			// if the tree is too deep, serialization will exceed the 7 levels limit.
			if (callTree.children == null)
			{
				items.Add(new TreeViewItem {id = id+1, depth = depth+1, displayName = "<Serialization Limit>"});
			}
			else
			{
				foreach (var parent in callTree.children)
				{
					BuildNode(items, parent, depth + 1);
				}
			}
		}
	}
}

