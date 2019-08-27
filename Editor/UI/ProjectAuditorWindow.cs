using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditorInternal;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor
{
    class ProjectAuditorWindow : EditorWindow
    {
        enum IssueCategory
        {
            ApiCalls,
            ProjectSettings
        }
        
        private ProjectAuditor m_ProjectAuditor;
        private ProjectReport m_ProjectReport;
        private IssueTable m_IssueTable;

        private bool m_EnableCPU = true;
        private bool m_EnableGPU = true;
        private bool m_EnableMemory = true;
        private bool m_EnableBuildSize = true;
        private bool m_EnableLoadTimes = true;
        private bool m_EnablePackages = false;
//        private bool m_EnableResolvedItems = false;

        private IssueCategory m_ActiveMode = IssueCategory.ApiCalls;

        string[] ReportModeStrings = {
            "API Calls",
            "Project Settings"
        };
        
        public static GUIStyle Toolbar;
        public static readonly GUIContent AnalyzeButton = new GUIContent("Analyze", "Analyze Project and list all issues found.");
        public static readonly GUIContent ReloadButton = new GUIContent("Reload DB", "Reload Issue Definition files.");
        public static readonly GUIContent SerializeButton = new GUIContent("Serialize", "Serialize project report to file.");

        private void OnEnable()
        {
            m_ProjectAuditor = new ProjectAuditor();
        }

        private void OnGUI()
        {
            Toolbar = "Toolbar";

            DrawToolbar();

            if (m_IssueTable != null)
            {
                var activeMode = m_ActiveMode;
                m_ActiveMode = (IssueCategory)GUILayout.Toolbar((int)m_ActiveMode, ReportModeStrings);

                if (activeMode != m_ActiveMode)
                {
                    // the user switched view
                    RefreshDisplay();
                }                    
                
                Rect r = EditorGUILayout.GetControlRect(GUILayout.ExpandHeight(true));
                m_IssueTable.OnGUI(r);

                DrawDetails();
            }
        }

        bool ShouldDisplay(ProjectIssue issue)
        {
            string category = issue.category;

            string url = issue.url;
            if (!m_EnablePackages && category.Contains("API Call") &&
                (url.Contains("Library/PackageCache/") || url.Contains("Resources/PackageManager/BuiltInPackages/")))
                return false;

// temporarily disabled Resolve Items button since there might be issues that have just been checked but are still shown in the list
//            if (!m_EnableResolvedItems && issue.resolved == true)
//                return false;

            string area = issue.def.area;
            if (m_EnableCPU && area.Contains("CPU"))
                return true;
            if (m_EnableGPU && area.Contains("GPU"))
                return true;
            if (m_EnableMemory && area.Contains("Memory"))
                return true;
            if (m_EnableBuildSize && area.Contains("Build Size"))
                return true;
            if (m_EnableLoadTimes && area.Contains("Load Times"))
                return true;

            return false;
        }

        private void Analyze()
        {
            m_ProjectReport = m_ProjectAuditor.Audit();
            RefreshDisplay();
        }

        private void RefreshDisplay()
        {
            if (m_ProjectReport == null)
                return;

            MultiColumnHeaderState.Column[] columns = new MultiColumnHeaderState.Column[]
            {
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Resolved?", "Issues that have already been looked at"),
                    width = 80,
                    minWidth = 80,
                    autoResize = true
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Area", "The area the issue might have an impact on"),
                    width = 100,
                    minWidth = 100,
                    autoResize = true
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Description", "Issue description"),
                    width = 300,
                    minWidth = 100,
                    autoResize = true
                },
                              
            };

            var columnsList = new List<MultiColumnHeaderState.Column>(columns);
            
            if (m_ActiveMode == IssueCategory.ApiCalls)
                columnsList.Add(new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Location", "Path to the script file"),
                    width = 900,
                    minWidth = 400,
                    autoResize = true
                } );

            var issues = m_ActiveMode == IssueCategory.ApiCalls
                ? m_ProjectReport.m_ApiCallsIssues
                : m_ProjectReport.m_ProjectSettingsIssues;
            
            var filteredList = issues.Where(x => ShouldDisplay(x));

            m_IssueTable = new IssueTable(new TreeViewState(),
                new MultiColumnHeader(new MultiColumnHeaderState(columnsList.ToArray())), filteredList.ToArray());
        }

        private void Reload()
        {
            m_ProjectAuditor.LoadDatabase();
            m_IssueTable = null;
        }

        private void Serialize()
        {
            if (m_ProjectReport != null)
                m_ProjectReport.WriteToFile();
        }

        private void DrawDetails()
        {            
            if (m_IssueTable.HasSelection())
            {               
                var issues = m_ActiveMode == IssueCategory.ApiCalls
                    ? m_ProjectReport.m_ApiCallsIssues
                    : m_ProjectReport.m_ProjectSettingsIssues;
                
                EditorStyles.textField.wordWrap = true;

                //            var index = m_IssueTable.GetSelection()[0];
                //            var issue = m_ProjectAuditor.m_ProjectIssues[index];

                var displayIndex = m_IssueTable.GetSelection()[0];
                int listIndex = 0;
                int i = 0;

                for (; i < issues.Count; ++i)
                {
                    if (ShouldDisplay(issues[i]))
                    {
                        if (listIndex == displayIndex)
                        {
                            break;
                        }
                        ++listIndex;
                    }
                }

                var issue = issues[i];

                // TODO: use an Issue interface, to define how to display different categories
                string text = string.Empty;

                text = $"Issue: {issue.def.problem}";
                EditorGUILayout.TextArea(text, GUILayout.Height(100)/*, GUILayout.ExpandHeight(true)*/ );

                text = $"Recommendation: {issue.def.solution}";
                EditorGUILayout.TextArea(text, GUILayout.Height(100)/*, GUILayout.ExpandHeight(true)*/);
                EditorStyles.textField.wordWrap = false;

            }
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(Toolbar);

            if (GUILayout.Button(AnalyzeButton, GUILayout.ExpandWidth(true), GUILayout.Width(80)))
                Analyze();
            if (GUILayout.Button(ReloadButton, GUILayout.ExpandWidth(true), GUILayout.Width(80)))
                Reload();
            if (GUILayout.Button(SerializeButton, GUILayout.ExpandWidth(true), GUILayout.Width(80)))
                Serialize();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal(Toolbar);
            GUILayout.Label("Filter By:", GUILayout.ExpandWidth(true), GUILayout.Width(80));

            EditorGUI.BeginChangeCheck();

            m_EnableMemory = EditorGUILayout.ToggleLeft("Memory", m_EnableMemory, GUILayout.Width(100));
            m_EnableCPU = EditorGUILayout.ToggleLeft("CPU", m_EnableCPU, GUILayout.Width(100));
            m_EnableGPU = EditorGUILayout.ToggleLeft("GPU", m_EnableGPU, GUILayout.Width(100));
            m_EnableBuildSize = EditorGUILayout.ToggleLeft("Build Size", m_EnableBuildSize, GUILayout.Width(100));
            m_EnableLoadTimes = EditorGUILayout.ToggleLeft("Load Times", m_EnableLoadTimes, GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal(Toolbar);
            GUILayout.Label("", GUILayout.ExpandWidth(true), GUILayout.Width(80));
            m_EnablePackages = EditorGUILayout.ToggleLeft("Packages", m_EnablePackages, GUILayout.Width(100));
//            m_EnableResolvedItems = EditorGUILayout.ToggleLeft("Resolved Items", m_EnableResolvedItems, GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();

            if (EditorGUI.EndChangeCheck())
            {
                RefreshDisplay();
            }
        }

        [MenuItem("Window/Analysis/Project Auditor")]
        public static ProjectAuditorWindow ShowWindow()
        {
            var wnd = GetWindow(typeof(ProjectAuditorWindow)) as ProjectAuditorWindow;
            if (wnd != null)
            {
                wnd.titleContent = EditorGUIUtility.TrTextContent("Project Auditor");
            }
            return wnd;
        }
    }
}