using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor
{
    class IssueTable : TreeView
    {
        enum ColumnIndex
        {
            Resolved = 0,
            Area,
            Description,
            Location
        }

        readonly List<TreeViewItem> m_Rows = new List<TreeViewItem>(100);

        ProjectIssue[] m_Issues;

        public IssueTable(TreeViewState state, MultiColumnHeader multicolumnHeader, ProjectIssue[] issues) : base(state, multicolumnHeader)
        {
            m_Issues = issues;
            Reload();
        }

        protected override TreeViewItem BuildRoot()
        {
            int idForhiddenRoot = -1;
            int depthForHiddenRoot = -1;
            var root = new IssueTableItem(idForhiddenRoot, depthForHiddenRoot, "root", new ProjectIssue());

            int index = 0;
            foreach (var issue in m_Issues)
            {
                var item = new IssueTableItem(index++, 0, "", issue);
                root.AddChild(item);            
            }

            return root;
        }

        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            m_Rows.Clear();

            if (rootItem != null && rootItem.children != null)
            {
                foreach (IssueTableItem node in rootItem.children)
                {
                    m_Rows.Add(node);
                }
            }

            // SortIfNeeded(m_Rows);

            return m_Rows;
        }
    
        protected override void RowGUI(RowGUIArgs args)
        {
            var item = (IssueTableItem)args.item;
            for (int i = 0; i < args.GetNumVisibleColumns(); ++i)
            {
                CellGUI(args.GetCellRect(i), i, item.m_projectIssue, ref args);
            }
        }

        void CellGUI(Rect cellRect, int column, ProjectIssue issue, ref RowGUIArgs args)
        {
            switch ((ColumnIndex)column)
            {
                case ColumnIndex.Resolved :
                    issue.resolved = EditorGUI.Toggle(cellRect, issue.resolved);
                    break;
                case ColumnIndex.Area :
                    EditorGUI.LabelField(cellRect, new GUIContent(issue.def.area, "This issue might have an impact on " + issue.def.area));
                    break;
                case ColumnIndex.Description :
                    string text = issue.def.type + "." + issue.def.method;
                    string tooltip = issue.def.problem + " \n\n" + issue.def.solution;
                    EditorGUI.LabelField(cellRect, new GUIContent(text, tooltip));
                    break;
                case ColumnIndex.Location :
                    var location = string.Format("{0}({1},{2})", issue.relativePath, issue.line,  issue.column);

                    if (location.StartsWith("Library/PackageCache/"))
                    {
                        location = location.Remove(0, "Library/PackageCache/".Length);
                    }
                    
                    // display fullpath as tooltip
                    EditorGUI.LabelField(cellRect, new GUIContent(location, issue.location));

                    break;
            
            }
        }

        protected override void DoubleClickedItem(int id)
        {
            var issue = m_Issues[id];
            if (issue.category.Equals(IssueCategory.ApiCalls.ToString()))
            {
                var obj = AssetDatabase.LoadAssetAtPath<TextAsset>(issue.relativePath);
            
                // Note that this this does not work with Package assets
                AssetDatabase.OpenAsset(obj, issue.line);                
            }
        }
    }
}