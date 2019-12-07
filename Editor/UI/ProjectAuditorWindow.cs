using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor
{
    internal class ProjectAuditorWindow : EditorWindow, IHasCustomMenu
    {       
        private ProjectAuditor m_ProjectAuditor;
        [SerializeField] private ProjectReport m_ProjectReport;
        
        private List<IssueTable> m_IssueTables = new List<IssueTable>();
		private CallHierarchyView m_CallHierarchyView;
        private CallTreeNode m_CurrentCallTree = null;

        private IssueTable m_ActiveIssueTable
        {
            get { return m_IssueTables[(int) m_ActiveMode]; }
        }
        
        private string[] m_AssemblyNames;

        private enum AssemblyIndex
        {
            None = -1,
            All = 0
        }
       
        private const string m_DefaultAssemblyName = "Assembly-CSharp";
        
        private SearchField m_SearchField;
        
        [SerializeField] private bool m_ValidReport = false;
        [SerializeField] private int m_ActiveAssembly = (int)AssemblyIndex.None;
        [SerializeField] private Area m_ActiveArea = Area.All;
        [SerializeField] private IssueCategory m_ActiveMode = IssueCategory.ApiCalls;
        [SerializeField] private bool m_ShowDetails = true;
        [SerializeField] private bool m_ShowRecommendation = true;
        [SerializeField] private bool m_ShowCallTree = false;
        
        [SerializeField] private string m_SearchText;
        [SerializeField] private bool m_DeveloperMode = false;
        
        static readonly string[] AreaEnumStrings = {
            "CPU",
            "GPU",
            "Memory",
            "Build Size",
            "Load Times",
            "All Areas"
        };

        private const int m_ToolbarWidth = 600;
        private const int m_FoldoutWidth = 300;
        private const int m_FoldoutMinHeight = 100;
        private const int m_FoldoutMaxHeight = 220;

        internal static class Styles
        {
            public static readonly GUIContent DeveloperMode = new GUIContent("Developer Mode");
            public static readonly GUIContent UserMode = new GUIContent("User Mode");
            
            public static readonly GUIContent WindowTitle = new GUIContent("Project Auditor");
            public static readonly GUIContent AnalyzeButton = new GUIContent("Analyze", "Analyze Project and list all issues found.");
            public static readonly GUIContent ReloadButton = new GUIContent("Reload DB", "Reload Issue Definition files.");
            public static readonly GUIContent ExportButton = new GUIContent("Export", "Export project report to .csv files.");

            public static readonly GUIContent MuteButton = new GUIContent("Mute", "Always ignore selected issues.");
            public static readonly GUIContent UnmuteButton = new GUIContent("Unmute", "Always show selected issues.");
                
            public static readonly GUIContent[] ColumnHeaders = {
                new GUIContent("Issue", "Issue description"),
                new GUIContent("Area", "The area the issue might have an impact on"),
                new GUIContent("Filename", "Filename and line number"),
                new GUIContent("Assembly", "Managed Assembly name")
            };

            public static readonly GUIContent DetailsFoldout = new GUIContent("Details", "Issue Details");
            public static readonly GUIContent RecommendationFoldout = new GUIContent("Recommendation", "Recommendation on how to solve the issue");
            public static readonly GUIContent CallTreeFoldout = new GUIContent("Inverted Call Hierarchy", "Inverted Call Hierarchy");
            
            public static readonly string HelpText =
@"Project Auditor is an experimental static analysis tool for Unity Projects.
This tool will analyze scripts and project settings of any Unity project
and report a list a possible problems that might affect performance, memory and other areas.

To Analyze the project:
* Click on Analyze.

Once the project is analyzed, the tool displays list of issues.
At the moment there are two types of issues: API calls or Project Settings. The tool allows the user to switch between the two.
In addition, it is possible to filter issues by area (CPU/Memory/etc...) or assembly name or search for a specific string.";
        }

        public static readonly string NoIssueSelectedText = "No issue selected";      
        
        private void OnEnable()
        {
            m_ProjectAuditor = new ProjectAuditor();    

            var assemblyNames = new List<string>(new []{"All Assemblies"});
            assemblyNames.AddRange(m_ProjectAuditor.GetAuditor<ScriptAuditor>().assemblyNames);
            m_AssemblyNames = assemblyNames.ToArray();

            if (m_ActiveAssembly == (int)AssemblyIndex.None)
            {
                m_ActiveAssembly = assemblyNames.IndexOf(m_DefaultAssemblyName);
                if (m_ActiveAssembly < 0)
                    m_ActiveAssembly = (int)AssemblyIndex.All;
            }

            m_CallHierarchyView = new CallHierarchyView(new TreeViewState());
            
            RefreshDisplay();
        }

        private void OnGUI()
        {
            DrawToolbar();
            DrawFilters();
            DrawIssues(); // and right-end panels
        }

        private void OnToggleDeveloperMode()
        {
            m_DeveloperMode = !m_DeveloperMode;
        }
        
        public virtual void AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(Styles.DeveloperMode, m_DeveloperMode, OnToggleDeveloperMode);
            menu.AddItem(Styles.UserMode, !m_DeveloperMode, OnToggleDeveloperMode);
        }

        private bool IsAnalysisValid()
        {
            return m_ValidReport;
        }
        
        public bool ShouldDisplay(ProjectIssue issue)
        {
            if (m_ActiveMode == IssueCategory.ApiCalls && m_ActiveAssembly != (int)AssemblyIndex.All && m_ActiveAssembly != (int)AssemblyIndex.None && !m_AssemblyNames[m_ActiveAssembly].Equals(issue.assembly))
            {
                return false;
            }
            
            if (m_ActiveArea != Area.All && !AreaEnumStrings[(int)m_ActiveArea].Equals(issue.descriptor.area))
            {
                return false;
            }

			if (!m_ProjectAuditor.config.displayMutedIssues)
            {
                var rule = m_ProjectAuditor.config.GetRule(issue.descriptor, issue.callingMethod);
                if (rule != null && rule.action == Rule.Action.None)
                    return false;
            }

            if (!string.IsNullOrEmpty(m_SearchText))
            {
                if (!MatchesSearch(issue.description) &&
                    !MatchesSearch(issue.filename) &&
                    !MatchesSearch(issue.name))
                {
                    return false;
                }
            }
            return true;
        }

        private bool MatchesSearch(string field)
        {
            return (!string.IsNullOrEmpty(field) && field.IndexOf(m_SearchText, StringComparison.CurrentCultureIgnoreCase) >= 0);
        }

        private void Analyze()
        {
            m_ProjectReport = m_ProjectAuditor.Audit(new ProgressBarDisplay());

            m_ValidReport = true;
            
            m_IssueTables.Clear();
            
            RefreshDisplay();
        }

        private IssueTable CreateIssueTable(IssueCategory issueCategory, TreeViewState state)
        {
            var columnsList = new List<MultiColumnHeaderState.Column>();
            var numColumns = (int) IssueTable.Column.Count;
            for (int i = 0; i < numColumns; i++)
            {
                int width = 80;
                int minWidth = 80;
                switch ((IssueTable.Column) i)
                {
                    case IssueTable.Column.Description :
                        width = 300;
                        minWidth = 100;
                        break;
                    case IssueTable.Column.Area :
                        width = 60;
                        minWidth = 50;
                        break;
                    case IssueTable.Column.Filename :
                        if (issueCategory == IssueCategory.ProjectSettings)
                        {
                            width = 0;
                            minWidth = 0;
                        }
                        else
                        {
                            width = 180;
                            minWidth = 100;                            
                        }
                        break;
                    case IssueTable.Column.Assembly :
                        if (issueCategory == IssueCategory.ProjectSettings)
                        {
                            width = 0;
                            minWidth = 0;
                        }
                        else
                        {
                            width = 180;
                            minWidth = 100;                            
                        }
                        break;
                }
                                
                columnsList.Add(new MultiColumnHeaderState.Column
                {
                    headerContent = Styles.ColumnHeaders[i],
                    width = width,
                    minWidth = minWidth,
                    autoResize = true
                } );
            }

            var issues = m_ProjectReport.GetIssues(issueCategory);

            return new IssueTable(state,
                new MultiColumnHeader(new MultiColumnHeaderState(columnsList.ToArray())),
                issues.ToArray(),
                issueCategory == IssueCategory.ApiCalls,
                m_ProjectAuditor,
                this);
        }

        private void RefreshDisplay()
        {
            if (!IsAnalysisValid())
                return;

            if (m_IssueTables.Count == 0)
            {
                for(int i = 0; i < (int)IssueCategory.NumCategories; ++i)
                {
                    IssueTable issueTable = CreateIssueTable((IssueCategory)i, new TreeViewState());
                    m_IssueTables.Add(issueTable);
                }               
            }
            
            m_ActiveIssueTable.Reload();
        }

        private void Reload()
        {
            m_ProjectAuditor.LoadDatabase();
            m_IssueTables.Clear();
        }

        private void Export()
        {
            if (IsAnalysisValid())
                m_ProjectReport.Export();
        }

        private void DrawIssues()
        {
            if (!IsAnalysisValid())
                return;

            var selectedItems = m_ActiveIssueTable.GetSelectedItems();
            var selectedIssues = selectedItems.Select(i => i.m_ProjectIssue).ToArray();
            string info = selectedIssues.Length  + " / " + m_ActiveIssueTable.NumIssues(m_ActiveMode) + " issues";

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();
            
            Rect r = EditorGUILayout.GetControlRect(GUILayout.ExpandHeight(true));
            m_ActiveIssueTable.OnGUI(r);

            EditorGUILayout.LabelField(info, GUILayout.ExpandWidth(true), GUILayout.Width(200));
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(GUILayout.Width(m_FoldoutWidth));

            DrawFoldouts();

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawFoldouts()
        {
            ProblemDescriptor problemDescriptor = null;
            var selectedItems = m_ActiveIssueTable.GetSelectedItems();
            var selectedDescriptors = selectedItems.Select(i => i.problemDescriptor);
            var selectedIssues = selectedItems.Select(i => i.m_ProjectIssue);
            // find out if all descriptors are the same
            var firstDescriptor = selectedDescriptors.FirstOrDefault();
            if (selectedDescriptors.Count() == selectedDescriptors.Count(d => d.id == firstDescriptor.id))
            {
                problemDescriptor = firstDescriptor;    
            }

            DrawDetailsFoldout(problemDescriptor);
            DrawRecommendationFoldout(problemDescriptor);
            if (m_ActiveMode == IssueCategory.ApiCalls)
            {
                CallTreeNode callTree = null;
                if (selectedIssues.Count() == 1)
                {
                    var issue = selectedIssues.First();
                    if (issue != null)
                    {
                        // get caller sub-tree
                        callTree = issue.callTree.GetChild();    
                    }
                }
                if (m_CurrentCallTree != callTree)
                {
                    m_CallHierarchyView.SetCallTree(callTree);
                    m_CallHierarchyView.Reload();
                    m_CurrentCallTree = callTree;
                }
 
                DrawCallHierarchy(callTree);
            }
        }

        private bool BoldFoldout(bool toggle, GUIContent content)
        {
            GUIStyle foldoutStyle = new GUIStyle(EditorStyles.foldout);
            foldoutStyle.fontStyle = FontStyle.Bold;
            return EditorGUILayout.Foldout(toggle, content, foldoutStyle);
        }

        private void DrawDetailsFoldout(ProblemDescriptor problemDescriptor)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(m_FoldoutWidth), GUILayout.MinHeight(m_FoldoutMinHeight));

            m_ShowDetails = BoldFoldout(m_ShowDetails, Styles.DetailsFoldout);
            if (m_ShowDetails)
            {
                if (problemDescriptor != null)
                {
                    EditorStyles.textField.wordWrap = true;
//                    var text = issue.description + " is called from " + issue.callingMethod + "\n\n" + issue.def.problem;
                    var text = problemDescriptor.problem;
					GUILayout.TextArea(text, GUILayout.MaxHeight(m_FoldoutMaxHeight));
                }
                else
                {
                    EditorGUILayout.LabelField(NoIssueSelectedText);
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawRecommendationFoldout(ProblemDescriptor problemDescriptor)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(m_FoldoutWidth), GUILayout.MinHeight(m_FoldoutMinHeight));

            m_ShowRecommendation = BoldFoldout(m_ShowRecommendation, Styles.RecommendationFoldout);
            if (m_ShowRecommendation)
            {
                if (problemDescriptor != null)
                {
                    EditorStyles.textField.wordWrap = true;
                    GUILayout.TextArea(problemDescriptor.solution, GUILayout.MaxHeight(m_FoldoutMaxHeight));
                }
                else
                {
                    EditorGUILayout.LabelField(NoIssueSelectedText);
                }
            }
            EditorGUILayout.EndVertical();
        }
        
        private void DrawCallHierarchy(CallTreeNode callTree)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(m_FoldoutWidth), GUILayout.MinHeight(m_FoldoutMinHeight*2));

            m_ShowCallTree = BoldFoldout(m_ShowCallTree, Styles.CallTreeFoldout);
            if (m_ShowCallTree)
            {
                if (callTree != null)
                {
                    Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(400));

                    m_CallHierarchyView.OnGUI(r);
                }
                else
                {
                    EditorGUILayout.LabelField(NoIssueSelectedText);
                }
            }
            EditorGUILayout.EndVertical();
        }
        
        private void DrawFilters()
        {
            if (!IsAnalysisValid())
                return;
            
            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(m_ToolbarWidth));

            {
                EditorGUILayout.BeginHorizontal();
                
                var assembly = EditorGUILayout.Popup(m_ActiveAssembly, m_AssemblyNames, GUILayout.MaxWidth(150));
                var area = (Area)EditorGUILayout.Popup((int)m_ActiveArea, AreaEnumStrings, GUILayout.MaxWidth(150));
                var mode = (IssueCategory)GUILayout.Toolbar((int)m_ActiveMode, m_ProjectAuditor.auditorNames, GUILayout.MaxWidth(150), GUILayout.ExpandWidth(true));

                EditorGUILayout.EndHorizontal();

                EditorGUI.BeginChangeCheck();

                var searchRect = GUILayoutUtility.GetRect(1, 1, 18, 18, GUILayout.ExpandWidth(true), GUILayout.Width(200));
                EditorGUILayout.BeginHorizontal();

                if(m_SearchField == null)
                {
                    m_SearchField = new SearchField();
                }

                m_SearchText = m_SearchField.OnGUI(searchRect, m_SearchText);
                
                EditorGUILayout.EndHorizontal();
                
				bool shouldRefresh = false;
                if (m_DeveloperMode)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Build :", GUILayout.ExpandWidth(true), GUILayout.Width(80));
                    m_ProjectAuditor.config.enableAnalyzeOnBuild = EditorGUILayout.ToggleLeft("Auto Analyze",
                        m_ProjectAuditor.config.enableAnalyzeOnBuild, GUILayout.Width(100));
                    m_ProjectAuditor.config.enableFailBuildOnIssues = EditorGUILayout.ToggleLeft("Fail on Issues",
                        m_ProjectAuditor.config.enableFailBuildOnIssues, GUILayout.Width(100));
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Selected :", GUILayout.ExpandWidth(true), GUILayout.Width(80));
                m_ProjectAuditor.config.displayMutedIssues = EditorGUILayout.ToggleLeft("Show Muted Issues", m_ProjectAuditor.config.displayMutedIssues, GUILayout.Width(120));
                if (GUILayout.Button(Styles.MuteButton, GUILayout.ExpandWidth(true), GUILayout.Width(100)))
                {
                    var selectedItems = m_ActiveIssueTable.GetSelectedItems();
                    foreach (IssueTableItem item in selectedItems)
                    {
                        SetRuleForItem(item, Rule.Action.None);
                    }

                    if (!m_ProjectAuditor.config.displayMutedIssues)
                    {
                        m_ActiveIssueTable.SetSelection(new List<int>());
                    }
                }
                if (GUILayout.Button(Styles.UnmuteButton, GUILayout.ExpandWidth(true), GUILayout.Width(100)))
                {
                    var selectedItems = m_ActiveIssueTable.GetSelectedItems();
                    foreach (IssueTableItem item in selectedItems)
                    {
                        ClearRulesForItem(item);
                    }
                }
                EditorGUILayout.EndHorizontal();

	            if (EditorGUI.EndChangeCheck())
	            {
    	            shouldRefresh = true;
        	    }

                if (shouldRefresh || m_ActiveArea != area  || m_ActiveMode != mode || !assembly.Equals(m_ActiveAssembly))
                {
                    m_ActiveAssembly = assembly;
                    m_ActiveArea = area;
                    m_ActiveMode = mode;
                    RefreshDisplay();
                }
            }
            EditorGUILayout.EndVertical();            
        }
        private void SetRuleForItem(IssueTableItem item, Rule.Action ruleAction)
        {
            var descriptor = item.problemDescriptor;

            string callingMethod = "";
            Rule rule;
            if (item.hasChildren)
            {
                rule = m_ProjectAuditor.config.GetRule(descriptor);
            }
            else
            {
                callingMethod = item.m_ProjectIssue.callingMethod;
                rule = m_ProjectAuditor.config.GetRule(descriptor, callingMethod);
            }

            if (rule == null)
            {
                m_ProjectAuditor.config.AddRule(new Rule
                {
                    id = descriptor.id,
                    filter = callingMethod,
                    action = ruleAction
                });                                           
            }
            else
            {
                rule.action = ruleAction;
            }
        }

        private void ClearRulesForItem(IssueTableItem item)
        {
            m_ProjectAuditor.config.ClearRules(item.problemDescriptor, item.hasChildren ? string.Empty : item.m_ProjectIssue.callingMethod);
        }
        
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(GUI.skin.box);
            {
                if (GUILayout.Button(Styles.AnalyzeButton, GUILayout.ExpandWidth(true), GUILayout.Width(80)))
                    Analyze();

                if (m_DeveloperMode)
                {
                    if (GUILayout.Button(Styles.ReloadButton, GUILayout.ExpandWidth(true), GUILayout.Width(80)))
                        Reload();                

                    // Export button needs to be properly tested before exposing it
                    if (IsAnalysisValid() && GUILayout.Button(Styles.ExportButton, GUILayout.ExpandWidth(true), GUILayout.Width(80)))
                        Export();
                }
            }
            EditorGUILayout.EndHorizontal();
            
            if (!IsAnalysisValid())
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);
         
                GUIStyle helpStyle = new GUIStyle(EditorStyles.textField);
                helpStyle.wordWrap = true;

                EditorGUILayout.LabelField(Styles.HelpText, helpStyle);
    
                EditorGUILayout.EndVertical();
            }
        }
        
#if UNITY_2018_1_OR_NEWER
        [MenuItem("Window/Analysis/Project Auditor")]
#else
        [MenuItem("Window/Project Auditor")]
#endif
        public static ProjectAuditorWindow ShowWindow()
        {
            var wnd = GetWindow(typeof(ProjectAuditorWindow)) as ProjectAuditorWindow;
            if (wnd != null)
            {
                wnd.titleContent = Styles.WindowTitle;
            }
            return wnd;
        }
    }
}
