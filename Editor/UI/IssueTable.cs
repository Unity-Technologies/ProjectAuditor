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
            Description = 0,
            // Resolved,
            Area,
            Location,

            Count
        }

        ProjectIssue[] m_Issues;

        bool m_GroupByDescription;

        public IssueTable(TreeViewState state, MultiColumnHeader multicolumnHeader, ProjectIssue[] issues, bool groupByDescription) : base(state, multicolumnHeader)
        {
            m_Issues = issues;
            m_GroupByDescription = groupByDescription;
            Reload();
        }

        protected override TreeViewItem BuildRoot()
        {
            int index = 0;
            int idForhiddenRoot = -1;
            int depthForHiddenRoot = -1;
            var root = new IssueTableItem(idForhiddenRoot, depthForHiddenRoot, "root");
            
            // grouped by assembly
            // HashSet<string> group = new HashSet<string>();
            // foreach (var issue in m_Issues)
            // {
            //     if (!string.IsNullOrEmpty(issue.assembly) && !assemblies.Contains(issue.assembly))
            //     {
            //         assemblies.Add(issue.assembly);
            //     }
            // }

            if (m_GroupByDescription)
            {
                // grouped by problem definition
                HashSet<string> allGroupsSet = new HashSet<string>();
                foreach (var issue in m_Issues)
                {
                    if (!allGroupsSet.Contains(issue.def.description))
                    {
                        allGroupsSet.Add(issue.def.description);
                    }
                }

                var allGroups = allGroupsSet.ToList();
                allGroups.Sort();

                foreach (var groupName in allGroups)
                {
                    // var issuesIngroup = m_Issues.Where(i => group.Equals(i.assembly));
                    var issues = m_Issues.Where(i => groupName.Equals(i.def.description));
                
                    // maybe dont create fake issue
                    // var assemblyGroup = new ProjectIssue
                    // {
                    //     assembly = assembly
                    // };
                    var groupItem = new IssueTableItem(index++, 0, groupName);
                    root.AddChild(groupItem); 
                    foreach (var issue in issues)
                    {
                        var item = new IssueTableItem(index++, 1, "Not Used", issue);
                        groupItem.AddChild(item);
                    }       
                }                
            }
            else
            {
                // flat view
               foreach (var issue in m_Issues)
               {
                   var item = new IssueTableItem(index++, 0, "", issue);
                   root.AddChild(item);            
               }
            }

            if (!root.hasChildren)
                root.AddChild(new IssueTableItem(index++, 0, "No elements found"));
            
            return root;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            for (int i = 0; i < args.GetNumVisibleColumns(); ++i)
            {
                CellGUI(args.GetCellRect(i), args.item, args.GetColumn(i), ref args);
            }
        }

        void CellGUI(Rect cellRect, TreeViewItem item, int column, ref RowGUIArgs args)
        {
            // only indent first column
            if (0 == column)
            {
                var indent = GetContentIndent(item) + extraSpaceBeforeIconAndLabel;
                cellRect.xMin += indent;
                CenterRectUsingSingleLineHeight(ref cellRect);                
            }

            var issue = (item as IssueTableItem).m_projectIssue;
            if (issue == null)
            {
                switch (column)
                {
                    case 0:
                        EditorGUI.LabelField(cellRect, new GUIContent(item.displayName, item.displayName));
                        break;
                }

                return;
            }

            switch ((Column)column)
            {
                // case Column.Resolved :
                //     issue.resolved = EditorGUI.Toggle(cellRect, issue.resolved);
                //     break;
                case Column.Area :
                    EditorGUI.LabelField(cellRect, new GUIContent(issue.def.area, "This issue might have an impact on " + issue.def.area));
                    break;
                case Column.Description :
                    string tooltip = issue.def.problem + " \n\n" + issue.def.solution;
                    EditorGUI.LabelField(cellRect, new GUIContent(issue.def.description, tooltip));
                    break;
                case Column.Location :
                    var location = string.Format("{0}({1},{2})", issue.relativePath, issue.line,  issue.column);

                    var libraryIndex = location.IndexOf("Library/PackageCache/");
                    if (libraryIndex >= 0)
                    {
                        location = location.Remove(0, libraryIndex + "Library/PackageCache/".Length);
                    }
                    
                    // display fullpath as tooltip
                    EditorGUI.LabelField(cellRect, new GUIContent(location, issue.location));

                    break;
            
            }
        }

        protected override void DoubleClickedItem(int id)
        {
            var rows = FindRows( new[] {id});
            var item = rows.FirstOrDefault();
            var issue = (item as IssueTableItem).m_projectIssue;
            if (issue.category.Equals(IssueCategory.ApiCalls.ToString()))
            {
                var obj = AssetDatabase.LoadAssetAtPath<TextAsset>(issue.relativePath);
            
                // Note that this this does not work with Package assets
                AssetDatabase.OpenAsset(obj, issue.line);                
            }
        }

        public IssueTableItem GetSelectedItem()
        {
            var ids = GetSelection();
            if (ids.Count() > 0)
            {
                var rows = FindRows(ids);
                if (rows.Count() > 0)
                    return rows[0] as IssueTableItem;                
            }

            return null;
        }
    }
}