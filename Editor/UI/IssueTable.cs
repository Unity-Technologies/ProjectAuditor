using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor
{
    class IssueTable : TreeView
    {
        public enum Column
        {
            Resolved = 0,
            Area,
            Description,
            Location,

            Count
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

            
            // flat view
//            int index = 0;
//            foreach (var issue in m_Issues)
//            {
//                var item = new IssueTableItem(index++, 0, "", issue);
//                root.AddChild(item);            
//            }

            // grouped by assembly
            HashSet<string> assemblies = new HashSet<string>();
            foreach (var issue in m_Issues)
            {
                if (!string.IsNullOrEmpty(issue.assembly) && !assemblies.Contains(issue.assembly))
                {
                    assemblies.Add(issue.assembly);
                }
            }

            int index = 0;
            foreach (var assembly in assemblies)
            {                
                var issuesInAssembly = m_Issues.Where(i => assembly.Equals(i.assembly));

                var assemblyGroup = new ProjectIssue
                {
                    assembly = assembly
                };
                var assemblyItem = new IssueTableItem(index++, 0, "ASM", assemblyGroup);
                root.AddChild(assemblyItem); 
                foreach (var issue in issuesInAssembly)
                {
                    var item = new IssueTableItem(index++, 1, "TEST", issue);
                   assemblyItem.AddChild(item);
                }       
            }

            return root;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var item = args.item;

            for (int i = 0; i < args.GetNumVisibleColumns(); ++i)
            {
                CellGUI(args.GetCellRect(i), args.item, args.GetColumn(i), ref args);
            }
        }

        void CellGUI(Rect cellRect, TreeViewItem item, int column, ref RowGUIArgs args)
        {
            var indent = GetContentIndent(item) + extraSpaceBeforeIconAndLabel;
            cellRect.xMin += indent;
            CenterRectUsingSingleLineHeight(ref cellRect);

            var issue = (item as IssueTableItem).m_projectIssue;
            if (issue.def == null)
            {
                switch ((Column) column)
                {
                    case Column.Resolved:
                        EditorGUI.LabelField(cellRect, new GUIContent(issue.assembly, issue.assembly));
                        break;
                }

                return;
            }

            switch ((Column)column)
            {
                case Column.Resolved :
                    issue.resolved = EditorGUI.Toggle(cellRect, issue.resolved);
                    break;
                case Column.Area :
                    EditorGUI.LabelField(cellRect, new GUIContent(issue.def.area, "This issue might have an impact on " + issue.def.area));
                    break;
                case Column.Description :
                    string text = issue.def.type + "." + issue.def.method;
                    string tooltip = issue.def.problem + " \n\n" + issue.def.solution;
                    EditorGUI.LabelField(cellRect, new GUIContent(text, tooltip));
                    break;
                case Column.Location :
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