using System;
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
            Area,
            Mute,
            Location,
            
            Count
        }

        internal static class Styles
        {
            public static readonly GUIContent MuteButton = new GUIContent("X", "Always ignore this type of issue.");
        }

        private ProjectAuditor m_ProjectAuditor;
        ProjectIssue[] m_Issues;

        bool m_GroupByDescription;

        public IssueTable(TreeViewState state, MultiColumnHeader multicolumnHeader, ProjectIssue[] issues, bool groupByDescription, ProjectAuditor projectAuditor) : base(state, multicolumnHeader)
        {
            m_ProjectAuditor = projectAuditor;
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
            if ((int)IssueTable.Column.Description == column)
            {
                var indent = GetContentIndent(treeViewItem) + extraSpaceBeforeIconAndLabel;
                cellRect.xMin += indent;
                CenterRectUsingSingleLineHeight(ref cellRect);                
            }

            var item = (treeViewItem as IssueTableItem);
            if (item == null)
                return;

            var issue = item.m_ProjectIssue;
            var descriptor = item.problemDescriptor;
            var areaLongDescription = "This issue might have an impact on " + descriptor.area;                      
            
            if (item.hasChildren)
            {
                switch ((Column)column)
                {
                    case Column.Mute:                        
                        if (GUI.Button(cellRect, Styles.MuteButton))
                        {                            
                            var rule = m_ProjectAuditor.config.GetRule(descriptor);
                            if (rule == null)
                            {
                                m_ProjectAuditor.config.AddRule(new Rule
                                {
                                    id = descriptor.id,
                                    action = Rule.Action.None
                                });                                           
                            }
                            else
                            {
                                rule.action = Rule.Action.None;
                            }
                        }
                        break;
                    case Column.Description:
                        EditorGUI.LabelField(cellRect, new GUIContent(item.displayName, item.displayName));
                        break;
                    case Column.Area:
                        EditorGUI.LabelField(cellRect, new GUIContent(descriptor.area, areaLongDescription));
                        break;
                }
            }
            else
            {
                var rule = m_ProjectAuditor.config.GetRule(descriptor, issue.callingMethodName);
                if (rule != null && rule.action == Rule.Action.None)
                    GUI.enabled = false;
                
                switch ((Column)column)
                {
                    case Column.Mute:                        
                        if (GUI.Button(cellRect, Styles.MuteButton))
                        {
                            if (rule == null)
                            {
                                m_ProjectAuditor.config.AddRule(new Rule
                                {
                                    id = descriptor.id,
                                    filter = issue.callingMethodName,
                                    action = Rule.Action.None
                                });
                            }
                            else
                            {
                                rule.action = Rule.Action.None;
                            }
                        }
                        break;
                    case Column.Area :
                        if (!m_GroupByDescription)
                            EditorGUI.LabelField(cellRect, new GUIContent(descriptor.area, areaLongDescription));
                        break;
                    case Column.Description :
                        if (m_GroupByDescription)
                        {
                            EditorGUI.LabelField(cellRect, new GUIContent(issue.callingMethodName, issue.callingMethod));
                        }
                        else
                        {
                            string tooltip = descriptor.problem + " \n\n" + descriptor.solution;
                            EditorGUI.LabelField(cellRect, new GUIContent(issue.description, tooltip));
                        }
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
                if (rule != null && rule.action == Rule.Action.None)
                    GUI.enabled = true;                
            }
        }

        protected override void DoubleClickedItem(int id)
        {
            var rows = FindRows( new[] {id});
            var item = rows.FirstOrDefault();
            if (!item.hasChildren)
            {
                var issue = (item as IssueTableItem).m_ProjectIssue;
                if (issue.category == IssueCategory.ApiCalls)
                {
                    var path = issue.relativePath;
                    if (path.StartsWith("Packages/") && path.Contains("@"))
                    {
                        // strip version from package path
                        var version = path.Substring(path.IndexOf("@"));
                        version = version.Substring(0, version.IndexOf("/"));
                        path = path.Replace(version, "");
                    }
                    var obj = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                    AssetDatabase.OpenAsset(obj, issue.line);                
                }              
            }
        }

        public IssueTableItem[] GetSelectedItems()
        {
            var ids = GetSelection();
            if (ids.Count() > 0)
            {
                return FindRows(ids).OfType<IssueTableItem>().ToArray();
            }

            return new IssueTableItem[0];
        }

        public int NumIssues(IssueCategory category)
        {
            return m_Issues.Where(i => i.category == category).Count();
        }
    }
}