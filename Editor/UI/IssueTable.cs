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
            Filename,

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
            var root = new TreeViewItem(idForhiddenRoot, depthForHiddenRoot, "root");
            
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
                    if (!allGroupsSet.Contains(issue.descriptor.description))
                    {
                        allGroupsSet.Add(issue.descriptor.description);
                    }
                }

                var allGroups = allGroupsSet.ToList();
                allGroups.Sort();

                foreach (var groupName in allGroups)
                {
                    // var issuesIngroup = m_Issues.Where(i => group.Equals(i.assembly));
                    var issues = m_Issues.Where(i => groupName.Equals(i.descriptor.description)).ToArray();
                    
                    // maybe dont create fake issue
                    // var assemblyGroup = new ProjectIssue
                    // {
                    //     assembly = assembly
                    // };

                    var displayName = string.Format("{0} ({1})", groupName, issues.Length);  
                    var groupItem = new IssueTableItem(index++, 0, displayName, issues.FirstOrDefault().descriptor);
                    root.AddChild(groupItem);
                    
                    foreach (var issue in issues)
                    {
                        var item = new IssueTableItem(index++, 1, "Not Used", issue.descriptor, issue);
                        groupItem.AddChild(item);
                    }       
                }                
            }
            else
            {
                // flat view
               foreach (var issue in m_Issues)
               {
                   var item = new IssueTableItem(index++, 0, "", issue.descriptor, issue);
                   root.AddChild(item);            
               }
            }

            if (!root.hasChildren)
                root.AddChild(new TreeViewItem(index++, 0, "No elements found"));
            
            return root;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            for (int i = 0; i < args.GetNumVisibleColumns(); ++i)
            {
                CellGUI(args.GetCellRect(i), args.item, args.GetColumn(i), ref args);
            }
        }

        void CellGUI(Rect cellRect, TreeViewItem treeViewItem, int column, ref RowGUIArgs args)
        {
            // only indent first column
            if (0 == column)
            {
                var indent = GetContentIndent(treeViewItem) + extraSpaceBeforeIconAndLabel;
                cellRect.xMin += indent;
                CenterRectUsingSingleLineHeight(ref cellRect);                
            }

            var item = (treeViewItem as IssueTableItem);
            if (item == null)
                return;

            var issue = item.m_ProjectIssue;
            var problemDescriptor = item.problemDescriptor;
            var areaLongDescription = "This issue might have an impact on " + problemDescriptor.area;                      
            
            if (item.hasChildren)
            {
                switch ((Column)column)
                {
                    case Column.Description:
                        EditorGUI.LabelField(cellRect, new GUIContent(item.displayName, item.displayName));
                        break;
                    case Column.Area:
                        EditorGUI.LabelField(cellRect, new GUIContent(problemDescriptor.area, areaLongDescription));
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
                    if (!m_GroupByDescription)
                        EditorGUI.LabelField(cellRect, new GUIContent(problemDescriptor.area, areaLongDescription));
                    break;
                case Column.Description :
                    if (m_GroupByDescription)
                    {
                        var callingMethod = issue.callingMethod;
                        var nameWithoutReturnTypeAndParameters = callingMethod.Substring(callingMethod.IndexOf(" "));
                        if (nameWithoutReturnTypeAndParameters.IndexOf("(") >= 0)
                            nameWithoutReturnTypeAndParameters = nameWithoutReturnTypeAndParameters.Substring(0, nameWithoutReturnTypeAndParameters.IndexOf("("));
                        
                        var name = nameWithoutReturnTypeAndParameters;
                        if (nameWithoutReturnTypeAndParameters.LastIndexOf("::") >= 0)
                        {
                            var onlyNamespace = nameWithoutReturnTypeAndParameters.Substring(0, nameWithoutReturnTypeAndParameters.LastIndexOf("::"));
                            if (onlyNamespace.LastIndexOf(".") >= 0)
                                name = nameWithoutReturnTypeAndParameters.Substring(onlyNamespace.LastIndexOf(".") + 1);
                        }
                        EditorGUI.LabelField(cellRect, new GUIContent(name, callingMethod));
                    }
                    else
                    {
                        string tooltip = problemDescriptor.problem + " \n\n" + problemDescriptor.solution;
                        EditorGUI.LabelField(cellRect, new GUIContent(issue.description, tooltip));
                    }
                    break;
                case Column.Location :
                    if (issue.filename != string.Empty)
                    {
                        var filename = string.Format("{0}:{1}", issue.filename, issue.line);

                        // display fullpath as tooltip
                        EditorGUI.LabelField(cellRect, new GUIContent(filename, issue.relativePath));                           
                    }
                    break;
            
            }
        }

        protected override void DoubleClickedItem(int id)
        {
            var rows = FindRows( new[] {id});
            var item = rows.FirstOrDefault();
            if (!item.hasChildren)
            {
                var issue = (item as IssueTableItem).m_ProjectIssue;
                var path = issue.relativePath;
                if (!string.IsNullOrEmpty(path))
                {
                    if ((path.StartsWith("Library/PackageCache") || path.StartsWith("Packages/") && path.Contains("@")))
                    {
                        // strip version from package path
                        var version = path.Substring(path.IndexOf("@"));
                        version = version.Substring(0, version.IndexOf("/"));
                        path = path.Replace(version, "").Replace("Library/PackageCache", "Packages");
                    }

                    var obj = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                    AssetDatabase.OpenAsset(obj, issue.line);                
                }              
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

        public int NumIssues()
        {
            return m_Issues.Length;
        }
    }
}
