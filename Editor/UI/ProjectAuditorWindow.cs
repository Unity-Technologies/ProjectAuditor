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
        private TreeViewSelection m_AssemblySelection = null;
        [SerializeField] private string m_AssemblySelectionSummary;
        private const string m_DefaultAssemblyName = "Assembly-CSharp";
        
        private SearchField m_SearchField;
        
        [SerializeField] private bool m_ValidReport = false;
        [SerializeField] private IssueCategory m_ActiveMode = IssueCategory.ApiCalls;
        [SerializeField] private bool m_ShowDetails = true;
        [SerializeField] private bool m_ShowRecommendation = true;
        [SerializeField] private bool m_ShowCallTree = false;
        
        [SerializeField] private string m_SearchText;
        [SerializeField] private bool m_DeveloperMode = false;
        
        private static readonly string[] m_AreaNames = {
            "CPU",
            "GPU",
            "Memory",
            "Build Size",
            "Load Times"
        };
        private TreeViewSelection m_AreaSelection = null;
        [SerializeField] private string m_AreaSelectionSummary;

        internal static class LayoutSize
        {
            public static readonly int ToolbarWidth = 600;
            public static readonly int FoldoutWidth = 300;
            public static readonly int FoldoutMinHeight = 100;
            public static readonly int FoldoutMaxHeight = 220;
            public static readonly int FilterOptionsLeftLabelWidth = 100;
            public static readonly int FilterOptionsEnumWidth = 50;
            public static readonly int ModeTabWidth = 300;
        };

        internal static class Styles
        {
            public static readonly GUIContent DeveloperMode = new GUIContent("Developer Mode");
            public static readonly GUIContent UserMode = new GUIContent("User Mode");
            
            public static readonly GUIContent WindowTitle = new GUIContent("Project Auditor");
            public static readonly GUIContent AnalyzeButton = new GUIContent("Analyze", "Analyze Project and list all issues found.");
            public static readonly GUIContent ReloadButton = new GUIContent("Reload DB", "Reload Issue Definition files.");
            public static readonly GUIContent ExportButton = new GUIContent("Export", "Export project report to .csv files.");
            
            public static readonly GUIContent assemblyFilter = new GUIContent("Assembly : ", "Select assemblies to examine");
            public static readonly GUIContent assemblyFilterSelect = new GUIContent("Select", "Select assemblies to examine");
            
            public static readonly GUIContent areaFilter = new GUIContent("Area : ", "Select performance areas to display");
            public static readonly GUIContent areaFilterSelect = new GUIContent("Select", "Select performance areas to display");

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

            var assemblyNames = new List<string>();
            assemblyNames.AddRange(m_ProjectAuditor.GetAuditor<ScriptAuditor>().assemblyNames);
            m_AssemblyNames = assemblyNames.ToArray();

            if (m_AssemblySelection == null)
            {
                m_AssemblySelection = new TreeViewSelection();
                
                if(!string.IsNullOrEmpty(m_AssemblySelectionSummary))
                {
                    if(m_AssemblySelectionSummary == "All")
                        m_AssemblySelection.SetAll(m_AssemblyNames);
                    else if (m_AssemblySelectionSummary != "None")
                    {
                        string[] assemblies = m_AssemblySelectionSummary.Split(new string[] { ", " }, StringSplitOptions.None);
                        foreach (string assembly in assemblies)
                        {
                            m_AssemblySelection.selection.Add(assembly);
                        }
                    }
                }
                else if (m_AssemblyNames.Contains(m_DefaultAssemblyName))
                {
                    m_AssemblySelection.Set(m_DefaultAssemblyName);    
                }
                else
                {
                    m_AssemblySelection.SetAll(m_AssemblyNames);
                }
            }

            if (m_AreaSelection == null)
            {
                m_AreaSelection = new TreeViewSelection();
                if(!string.IsNullOrEmpty(m_AreaSelectionSummary))
                {
                    if(m_AreaSelectionSummary == "All")
                        m_AreaSelection.SetAll(m_AreaNames);
                    else if (m_AreaSelectionSummary != "None")
                    {
                        string[] areas = m_AreaSelectionSummary.Split(new string[] { ", " }, StringSplitOptions.None);
                        foreach (string area in areas)
                        {
                            m_AreaSelection.selection.Add(area);
                        }
                    }
                }
                else
                {
                    m_AreaSelection.SetAll(m_AreaNames);    
                }
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
            if (m_ActiveMode == IssueCategory.ApiCalls &&
                !m_AssemblySelection.Contains(issue.assembly) &&
                !m_AssemblySelection.ContainsGroup("All"))
            {
                return false;
            }

            if (!m_AreaSelection.Contains(issue.descriptor.area) &&
                !m_AreaSelection.ContainsGroup("All"))
            {
                return false;
            }

			if (!m_ProjectAuditor.config.displayMutedIssues)
            {
                if (m_ProjectAuditor.config.GetAction(issue.descriptor, issue.callingMethod) == Rule.Action.None)
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
            {
                string path = EditorUtility.SaveFilePanel("Save analysis CSV data", "", "project-auditor-report.csv", "csv");
                if (path.Length != 0)
                {
                    m_ProjectReport.Export(path);
                }
            }    
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

            EditorGUILayout.BeginVertical(GUILayout.Width(LayoutSize.FoldoutWidth));

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
            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(LayoutSize.FoldoutWidth), GUILayout.MinHeight(LayoutSize.FoldoutMinHeight));

            m_ShowDetails = BoldFoldout(m_ShowDetails, Styles.DetailsFoldout);
            if (m_ShowDetails)
            {
                if (problemDescriptor != null)
                {
                    EditorStyles.textField.wordWrap = true;
//                    var text = issue.description + " is called from " + issue.callingMethod + "\n\n" + issue.def.problem;
                    var text = problemDescriptor.problem;
					GUILayout.TextArea(text, GUILayout.MaxHeight(LayoutSize.FoldoutMaxHeight));
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
            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(LayoutSize.FoldoutWidth), GUILayout.MinHeight(LayoutSize.FoldoutMinHeight));

            m_ShowRecommendation = BoldFoldout(m_ShowRecommendation, Styles.RecommendationFoldout);
            if (m_ShowRecommendation)
            {
                if (problemDescriptor != null)
                {
                    EditorStyles.textField.wordWrap = true;
                    GUILayout.TextArea(problemDescriptor.solution, GUILayout.MaxHeight(LayoutSize.FoldoutMaxHeight));
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
            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(LayoutSize.FoldoutWidth), GUILayout.MinHeight(LayoutSize.FoldoutMinHeight*2));

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
        
        string GetSelectedAssembliesSummary()
        {
            return GetSelectedSummary(m_AssemblySelection, m_AssemblyNames);
        }
        
        string GetSelectedAreasSummary()
        {
            return GetSelectedSummary(m_AreaSelection, m_AreaNames);
        }

        // SteveM TODO - This seems wildly more complex than it needs to be... UNLESS assemblies can have sub-assemblies?
        // If that's the case, we need to test for that. Otherwise we need to strip a bunch of this complexity out.
        string GetSelectedSummary(TreeViewSelection selection, string[] names)
        {
            if (selection.selection == null || selection.selection.Count == 0)
                return "None";

            // Count all items in a group
            var dict = new Dictionary<string, int>();
            var selectionDict = new Dictionary<string, int>();
            foreach (var nameWithIndex in names)
            {
                var identifier = new TreeItemIdentifier(nameWithIndex);
                if (identifier.index == TreeItemIdentifier.kAll)
                    continue;

                int count;
                if (dict.TryGetValue(identifier.name, out count))
                    dict[identifier.name] = count + 1;
                else
                    dict[identifier.name] = 1;

                selectionDict[identifier.name] = 0;
            }

            // Count all the items we have 'selected' in a group
            foreach (var nameWithIndex in selection.selection)
            {
                var identifier = new TreeItemIdentifier(nameWithIndex);

                if (dict.ContainsKey(identifier.name) &&
                    selectionDict.ContainsKey(identifier.name) &&
                    identifier.index <= dict[identifier.name])
                {
                    // Selected thread valid and in the thread list
                    // and also within the range of valid threads for this data set
                    selectionDict[identifier.name]++;
                }
            }

            // Count all groups where we have 'selected all the items'
            int selectedCount = 0;
            foreach (var name in dict.Keys)
            {
                if (selectionDict[name] != dict[name])
                    continue;

                selectedCount++;
            }
            
            // If we've just added all the item names we have everything selected
            // Note we don't compare against the names array directly as this contains the 'all' versions
            if (selectedCount == dict.Keys.Count)
                return "All";

            // Add all the individual items were we haven't already added the group
            List<string> individualItems = new List<string>();
            foreach (var name in selectionDict.Keys)
            {
                int selectionCount = selectionDict[name];
                if (selectionCount <= 0)
                    continue;
                int itemCount = dict[name];
                if (itemCount == 1)
                    individualItems.Add(name);
                else if (selectionCount != itemCount)
                    individualItems.Add(string.Format("{0} ({1} of {2})", name, selectionCount, itemCount));
                else
                    individualItems.Add(string.Format("{0} (All)", name));
            }

            // Maintain alphabetical order
            individualItems.Sort(CompareUINames);

            if (individualItems.Count == 0)
                return "None";

            string selectedText = string.Join(", ", individualItems.ToArray());
            return selectedText;
        }
        
        private int CompareUINames(string a, string b)
        {
            string[] aTokens = a.Split(':');
            string[] bTokens = b.Split(':');

            if (aTokens.Length > 1 && bTokens.Length > 1)
            {
                var aThreadName = aTokens[0].Trim();
                var bThreadName = bTokens[0].Trim();

                if (aThreadName == bThreadName)
                {
                    string aThreadIndex = aTokens[1].Trim();
                    string bThreadIndex = bTokens[1].Trim();

                    if (aThreadIndex == "All" && bThreadIndex != "All")
                        return -1;
                    if (aThreadIndex != "All" && bThreadIndex == "All")
                        return 1;

                    int aGroupIndex;
                    if (int.TryParse(aThreadIndex, out aGroupIndex))
                    {
                        int bGroupIndex;
                        if (int.TryParse(bThreadIndex, out bGroupIndex))
                        {
                            return aGroupIndex.CompareTo(bGroupIndex);
                        }
                    }
                }
            }

            return a.CompareTo(b);
        }
        
        private void DrawSelectedText(string text)
        {
#if UNITY_2019_1_OR_NEWER
            GUIStyle treeViewSelectionStyle = "TV Selection";
            GUIStyle backgroundStyle = new GUIStyle(treeViewSelectionStyle);

            GUIStyle treeViewLineStyle = "TV Line";
            GUIStyle textStyle = new GUIStyle(treeViewLineStyle);
#else
            GUIStyle textStyle = GUI.skin.label;
#endif

            GUIContent content = new GUIContent(text, text);
            Vector2 size = textStyle.CalcSize(content);
            Rect rect = EditorGUILayout.GetControlRect(GUILayout.MaxWidth(size.x), GUILayout.Height(size.y));
            if (Event.current.type == EventType.Repaint)
            {
#if UNITY_2019_1_OR_NEWER
                backgroundStyle.Draw(rect, false, false, true, true);
#endif
                GUI.Label(rect, content, textStyle);
            }
        }

        private void DrawAssemblyFilter()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(Styles.assemblyFilter, GUILayout.Width(LayoutSize.FilterOptionsLeftLabelWidth));
            
            if (m_AssemblyNames.Length > 0)
            {
                bool lastEnabled = GUI.enabled;
                // SteveM TODO - We don't currently have any sense of when the Auditor is busy and should disallow user input
                bool enabled = /*!IsAnalysisRunning() &&*/ !AssemblySelectionWindow.IsOpen();
                GUI.enabled = enabled;
                if (GUILayout.Button(Styles.assemblyFilterSelect, EditorStyles.miniButton, GUILayout.Width(LayoutSize.FilterOptionsEnumWidth)))
                {
                    // Note: Window auto closes as it loses focus so this isn't strictly required
                    if (AssemblySelectionWindow.IsOpen())
                    {
                        AssemblySelectionWindow.CloseAll();
                    }
                    else
                    {
                        Vector2 windowPosition = new Vector2(Event.current.mousePosition.x + LayoutSize.FilterOptionsEnumWidth, Event.current.mousePosition.y + GUI.skin.label.lineHeight);
                        Vector2 screenPosition = GUIUtility.GUIToScreenPoint(windowPosition);

                        AssemblySelectionWindow.Open(screenPosition.x, screenPosition.y, this, m_AssemblySelection, m_AssemblyNames);
                    }
                }

                GUI.enabled = lastEnabled;
                
                m_AssemblySelectionSummary = GetSelectedAssembliesSummary();
                DrawSelectedText(m_AssemblySelectionSummary);
                
                GUILayout.FlexibleSpace();
            }

            EditorGUILayout.EndHorizontal();
        }
        
        // SteveM TODO - if AssemblySelectionWindow and AreaSelectionWindow end up sharing a common base class then
        // DrawAssemblyFilter() and DrawAreaFilter() can be made to call a common method and just pass the selection, names
        // and the type of window we want.
        private void DrawAreaFilter()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(Styles.areaFilter, GUILayout.Width(LayoutSize.FilterOptionsLeftLabelWidth));
            
            if (m_AreaNames.Length > 0)
            {
                bool lastEnabled = GUI.enabled;
                // SteveM TODO - We don't currently have any sense of when the Auditor is busy and should disallow user input
                bool enabled = /*!IsAnalysisRunning() &&*/ !AreaSelectionWindow.IsOpen();
                GUI.enabled = enabled;
                if (GUILayout.Button(Styles.areaFilterSelect, EditorStyles.miniButton, GUILayout.Width(LayoutSize.FilterOptionsEnumWidth)))
                {
                    // Note: Window auto closes as it loses focus so this isn't strictly required
                    if (AreaSelectionWindow.IsOpen())
                    {
                        AreaSelectionWindow.CloseAll();
                    }
                    else
                    {
                        Vector2 windowPosition = new Vector2(Event.current.mousePosition.x + LayoutSize.FilterOptionsEnumWidth, Event.current.mousePosition.y + GUI.skin.label.lineHeight);
                        Vector2 screenPosition = GUIUtility.GUIToScreenPoint(windowPosition);

                        AreaSelectionWindow.Open(screenPosition.x, screenPosition.y, this, m_AreaSelection, m_AreaNames);
                    }
                }

                GUI.enabled = lastEnabled;
                
                m_AreaSelectionSummary = GetSelectedAreasSummary();
                DrawSelectedText(m_AreaSelectionSummary);
                
                GUILayout.FlexibleSpace();
            }

            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawFilters()
        {
            if (!IsAnalysisValid())
                return;
            
            EditorGUILayout.BeginVertical(GUI.skin.box/*, GUILayout.Width(LayoutSize.ToolbarWidth), GUILayout.ExpandWidth(true)*/);

            {
                EditorGUILayout.BeginHorizontal();
                
                var mode = (IssueCategory)GUILayout.Toolbar((int)m_ActiveMode, m_ProjectAuditor.auditorNames, GUILayout.MaxWidth(LayoutSize.ModeTabWidth)/*, GUILayout.ExpandWidth(true)*/);

                EditorGUILayout.EndHorizontal();

                DrawAssemblyFilter();
                DrawAreaFilter();
                
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

                if (shouldRefresh || m_ActiveMode != mode)
                {
                    m_ActiveMode = mode;
                    RefreshDisplay();
                }
            }
            EditorGUILayout.EndVertical();            
        }
        
        public void SetAssemblySelection(TreeViewSelection selection)
        {
            m_AssemblySelection = selection;
            RefreshDisplay();
        }
        
        public void SetAreaSelection(TreeViewSelection selection)
        {
            m_AreaSelection = selection;
            RefreshDisplay();
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

                GUI.enabled = IsAnalysisValid();
                if (GUILayout.Button(Styles.ExportButton, GUILayout.ExpandWidth(true), GUILayout.Width(80)))
                    Export();
                GUI.enabled = true;

                if (m_DeveloperMode)
                {
                    if (GUILayout.Button(Styles.ReloadButton, GUILayout.ExpandWidth(true), GUILayout.Width(80)))
                        Reload();                
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
