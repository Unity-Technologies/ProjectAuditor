using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor
{
    class ProjectAuditorWindow : EditorWindow, IHasCustomMenu
    {
        private bool m_DeveloperMode = false;
        
        private ProjectAuditor m_ProjectAuditor;
        private ProjectReport m_ProjectReport;
        private List<IssueTable> m_IssueTables = new List<IssueTable>();

        private IssueTable m_ActiveIssueTable
        {
            get { return m_IssueTables[(int) m_ActiveMode]; }
        }
        
        private List<bool> m_EnableAreas = new List<bool>();
//        private bool m_EnableResolvedItems = false;

        private bool m_ShowFilters = true;
        private bool m_ShowDetails = true;
        private bool m_ShowRecommendation = true;
        private bool m_ShowCallTree = false;

        private IssueCategory m_ActiveMode = IssueCategory.ApiCalls;
        
        static readonly string[] AreaEnumStrings = {
            "CPU",
            "GPU",
            "Memory",
            "Build Size",
            "Load Times",

        };
        
        private const int m_FoldoutWidth = 300;
        private const int m_FoldoutMaxHeight = 220;

        internal static class Styles
        {
            public static readonly GUIContent DeveloperMode = new GUIContent("Developer Mode");
            public static readonly GUIContent UserMode = new GUIContent("User Mode");
            
            public static readonly GUIStyle Toolbar = "Toolbar";
            public static readonly GUIContent WindowTitle = new GUIContent("Project Auditor");
            public static readonly GUIContent AnalyzeButton = new GUIContent("Analyze", "Analyze Project and list all issues found.");
            public static readonly GUIContent ReloadButton = new GUIContent("Reload DB", "Reload Issue Definition files.");
            public static readonly GUIContent ExportButton = new GUIContent("Export", "Export project report to json file.");
            public static readonly GUIContent MarkAsReadButton = new GUIContent("Mark As Read", "Mark this issue as read.");
            public static readonly GUIContent MarkAsUnreadButton = new GUIContent("Mark As Unread", "Mark this issue as unread.");
            public static readonly GUIContent IgnoreIssue = new GUIContent("Ignore Issue", "Always ignore this specific issue.");
            public static readonly GUIContent IgnoreIssueType = new GUIContent("Ignore Type", "Always ignore this type of issue.");

            public static readonly GUIContent[] ColumnHeaders = {
                new GUIContent("Issue", "Issue description"),
                // new GUIContent("Resolved?", "Issues that have already been looked at"),
                new GUIContent("Area", "The area the issue might have an impact on"),
                new GUIContent("Location", "Path to the script file")            
            };

            public static readonly GUIContent FiltersFoldout = new GUIContent("Filters", "Filters");
            public static readonly GUIContent DetailsFoldout = new GUIContent("Details", "Issue Details");
            public static readonly GUIContent RecommendationFoldout = new GUIContent("Recommendation", "Recommendation on how to solve the issue");
            public static readonly GUIContent CallTreeFoldout = new GUIContent("Call Tree", "Call Tree");
            
            public static readonly string HelpText =
@"Project Auditor is an experimental static analysis tool for Unity Projects.
This tool will analyze scripts and project settings of any Unity project
and report a list a possible problems that might affect performance, memory and other areas.

To Analyze the project:
* Click on Analyze.

Once the project is analyzed, the tool displays list of issues.
At the moment there are two types of issues: API calls or Project Settings. The tool allows the user to switch between the two.
In addition, it is possible to filter issues by area (CPU/Memory/etc...).

To generate a report, click on the Export button.
To reload the issue database definition, click on Reload DB. (Developer Mode only)";
        }

        public static readonly string NoIssueSelectedText = "No issue selected";
        
        private void OnEnable()
        {
            m_ProjectAuditor = new ProjectAuditor();
            
            var enumAreas = Enum.GetValues(typeof(Area));
            foreach(var area in enumAreas)
                m_EnableAreas.Add(true);
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

        bool IsAnalysisValid()
        {
            return m_ProjectReport != null;
        }
        
        bool ShouldDisplay(ProjectIssue issue)
        {
            string url = issue.url;
            if (!m_ProjectAuditor.config.enablePackages && issue.category == IssueCategory.ApiCalls &&
                (url.Contains("Library/PackageCache/") || url.Contains("Resources/PackageManager/BuiltInPackages/")))
                return false;

// temporarily disabled Resolve Items button since there might be issues that have just been checked but are still shown in the list
//            if (!m_EnableResolvedItems && issue.resolved == true)
//                return false;

            string area = issue.descriptor.area;
            for (int index=0;index < Enum.GetValues(typeof(Area)).Length; index++)
            {
                if (m_EnableAreas[index] && area.Contains(AreaEnumStrings[index]))
                    return true;                
            }

            return false;
        }

        private void Analyze()
        {
            m_ProjectReport = new ProjectReport();

            m_ProjectAuditor.Audit(m_ProjectReport);

            RefreshDisplay();
        }

        IssueTable CreateIssueTable(IssueCategory issueCategory)
        {
            var columnsList = new List<MultiColumnHeaderState.Column>();
            var numColumns = (int) IssueTable.Column.Count;
            for (int i = 0; i < numColumns; i++)
            {
                bool add = true;
                int width = 80;
                int minWidth = 80;
                switch ((IssueTable.Column) i)
                {
                    case IssueTable.Column.Description :
                        width = 300;
                        minWidth = 100;
                        break;
                    // case IssueTable.Column.Resolved :
                    //     width = 80;
                    //     minWidth = 80;
                    //     break;
                    case IssueTable.Column.Area :
                        width = 50;
                        minWidth = 50;
                        break;
                    case IssueTable.Column.Location :
                        if (issueCategory == IssueCategory.ProjectSettings)
                            add = false;
                        width = 900;
                        minWidth = 400;
                        break;
                }
                                
                if (add)
                    columnsList.Add(new MultiColumnHeaderState.Column
                    {
                        headerContent = Styles.ColumnHeaders[i],
                        width = width,
                        minWidth = minWidth,
                        autoResize = true
                    } );
            }

            var issues = m_ProjectReport.GetIssues(issueCategory);
            
            var filteredList = issues.Where(x => ShouldDisplay(x));
            
            return new IssueTable(new TreeViewState(),
                new MultiColumnHeader(new MultiColumnHeaderState(columnsList.ToArray())), filteredList.ToArray(), issueCategory == IssueCategory.ApiCalls);
        }

        private void RefreshDisplay()
        {
            if (!IsAnalysisValid())
                return;

            m_IssueTables.Clear();
            
            foreach (IssueCategory category in Enum.GetValues(typeof(IssueCategory)))
                m_IssueTables.Add(CreateIssueTable(category));                
        }

        private void Reload()
        {
            m_ProjectAuditor.LoadDatabase();
            m_IssueTables.Clear();
        }

        private void Export()
        {
            if (IsAnalysisValid())
                m_ProjectReport.WriteToFile();
        }

        private void DrawIssues()
        {
            if (IsAnalysisValid())
            {
                m_ActiveMode = (IssueCategory)GUILayout.Toolbar((int)m_ActiveMode, m_ProjectAuditor.auditorNames);

                EditorGUILayout.LabelField("Issues : " + m_ActiveIssueTable.NumIssues(), GUILayout.ExpandWidth(true), GUILayout.Width(80));
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.BeginVertical();

                Rect r = EditorGUILayout.GetControlRect(GUILayout.ExpandHeight(true));
                m_ActiveIssueTable.OnGUI(r);

                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical(GUILayout.Width(m_FoldoutWidth));

                DrawFoldouts();

                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
            }     
        }
        
        private void DrawFoldouts()
        {
            ProblemDescriptor problemDescriptor = null;
            var selectedItems = m_ActiveIssueTable.GetSelectedItems();
            var selectedDescriptors = selectedItems.Select(i => i.problemDescriptor);
            var selectedIssues = selectedItems.Select(i => i.m_ProjectIssue);
            // find out if all descriptors are the same
            var firstDescriptor = selectedDescriptors.FirstOrDefault();
            if (selectedDescriptors.Count() == selectedDescriptors.Where(d => d.id == firstDescriptor.id).Count())
            {
                problemDescriptor = firstDescriptor;    
            }

            bool enableMarkReadButton = true;
            var firstIssue = selectedIssues.FirstOrDefault();
            if (firstIssue == null || selectedIssues.Count() != selectedIssues.Where(i => i.markedAsRead == firstIssue.markedAsRead).Count())
            {
                enableMarkReadButton = false;
            }

            DrawDetailsFoldout(problemDescriptor);
            DrawRecommendationFoldout(problemDescriptor);
//            if (m_ActiveMode == IssueCategory.ApiCalls)
//                DrawCallTree(selectedIssue);

            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(m_FoldoutWidth));
            EditorGUILayout.BeginHorizontal();

            if (!enableMarkReadButton)
                GUI.enabled = false;
            if (GUILayout.Button((firstIssue != null && firstIssue.markedAsRead) ? Styles.MarkAsUnreadButton : Styles.MarkAsReadButton, GUILayout.Height(40), GUILayout.ExpandWidth(true), GUILayout.Width(100)))
            {
                foreach (var issue in selectedIssues)
                {
                    issue.markedAsRead = !issue.markedAsRead;
                }
            }
            if (!enableMarkReadButton)
                GUI.enabled = true;

//            GUI.enabled = false;
//            if (GUILayout.Button(Styles.IgnoreIssue, GUILayout.Height(40), GUILayout.ExpandWidth(true), GUILayout.Width(100)))
//            {               
//            }
//            GUI.enabled = true;
            if (GUILayout.Button(Styles.IgnoreIssueType, GUILayout.Height(40), GUILayout.ExpandWidth(true), GUILayout.Width(100)))
            {
                foreach (var descriptor in selectedDescriptors)
                {
                    m_ProjectAuditor.config.exceptions.Add(descriptor.id);                    
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private bool BoldFoldout(bool toggle, GUIContent content)
        {
            GUIStyle foldoutStyle = new GUIStyle(EditorStyles.foldout);
            foldoutStyle.fontStyle = FontStyle.Bold;
            return EditorGUILayout.Foldout(toggle, content, foldoutStyle);
        }

        private void DrawDetailsFoldout(ProblemDescriptor problemDescriptor)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(m_FoldoutWidth));

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
            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(m_FoldoutWidth));

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

        private void DrawCallTree(ProjectIssue issue)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(m_FoldoutWidth));

            m_ShowCallTree = BoldFoldout(m_ShowCallTree, Styles.CallTreeFoldout);
            if (m_ShowCallTree)
            {
                if (issue != null)
                {
                    // display method name without return type
                    EditorGUILayout.LabelField(issue.callingMethod.Substring(issue.callingMethod.IndexOf(" ")));
                }
                else
                {
                    EditorGUILayout.LabelField(NoIssueSelectedText);
                }
            }
            EditorGUILayout.EndVertical();
        }

        void DrawFilters()
        {
            if (!IsAnalysisValid())
                return;
            
            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(m_FoldoutWidth));

            m_ShowFilters = BoldFoldout(m_ShowFilters, Styles.FiltersFoldout);
            if (m_ShowFilters)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Filter By :", GUILayout.ExpandWidth(true), GUILayout.Width(80));

                EditorGUI.BeginChangeCheck();

                for (int index=0;index < Enum.GetValues(typeof(Area)).Length; index++)
                {
                    m_EnableAreas[index] = EditorGUILayout.ToggleLeft(AreaEnumStrings[index], m_EnableAreas[index], GUILayout.Width(100));
                }

                EditorGUILayout.EndHorizontal();

                if (m_DeveloperMode)
                {
#if UNITY_2018_1_OR_NEWER
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Include :", GUILayout.ExpandWidth(true), GUILayout.Width(80));
                    m_ProjectAuditor.config.enablePackages = EditorGUILayout.ToggleLeft("Packages", m_ProjectAuditor.config.enablePackages, GUILayout.Width(100));
    
                    //            m_EnableResolvedItems = EditorGUILayout.ToggleLeft("Resolved Items", m_EnableResolvedItems, GUILayout.Width(100));
                    EditorGUILayout.EndHorizontal();
#endif
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Build :", GUILayout.ExpandWidth(true), GUILayout.Width(80));
                    m_ProjectAuditor.config.enableAnalyzeOnBuild = EditorGUILayout.ToggleLeft("Auto Analyze", m_ProjectAuditor.config.enableAnalyzeOnBuild, GUILayout.Width(100));
                    m_ProjectAuditor.config.enableFailBuildOnIssues = EditorGUILayout.ToggleLeft("Fail on Issues", m_ProjectAuditor.config.enableFailBuildOnIssues, GUILayout.Width(100));
                    EditorGUILayout.EndHorizontal();
                }

                if (EditorGUI.EndChangeCheck())
                {
                    RefreshDisplay();
                }
            }
            EditorGUILayout.EndVertical();            
        }
        
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(Styles.Toolbar);

            if (GUILayout.Button(Styles.AnalyzeButton, GUILayout.ExpandWidth(true), GUILayout.Width(80)))
                Analyze();
            
            if (m_DeveloperMode)
                if (GUILayout.Button(Styles.ReloadButton, GUILayout.ExpandWidth(true), GUILayout.Width(80)))
                    Reload();

            if (!IsAnalysisValid())
            {
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginVertical(GUI.skin.box);
         
                GUIStyle helpStyle = new GUIStyle(EditorStyles.textField);
                helpStyle.wordWrap = true;

                EditorGUILayout.LabelField(Styles.HelpText, helpStyle);
                EditorGUILayout.EndHorizontal();                
            }
            else
            {
                // Export button needs to be properly tested before exposing it
                if (m_DeveloperMode)
                    if (GUILayout.Button(Styles.ExportButton, GUILayout.ExpandWidth(true), GUILayout.Width(80)))
                        Export();

                EditorGUILayout.EndHorizontal();
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
