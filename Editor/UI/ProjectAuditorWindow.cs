using System;
using System.Collections.Generic;
using System.Linq;
using Unity.ProjectAuditor.Editor.CodeAnalysis;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor
{
    internal interface IIssuesFilter
    {
        bool Match(ProjectIssue issue);
    }

    internal class ProjectAuditorWindow : EditorWindow, IHasCustomMenu, IIssuesFilter
    {
        enum AnalysisState
        {
            NotStarted,
            InProgress,
            Completed,
            Valid
        }
        // strings
        private static readonly string[] m_AreaNames =
        {
            "CPU",
            "GPU",
            "Memory",
            "Build Size",
            "Load Times"
        };

        private static readonly string NoIssueSelectedText = "No issue selected";

        private readonly AnalysisViewDescriptor[] m_AnalysisViewDescriptors =
        {
            new AnalysisViewDescriptor
            {
                category = IssueCategory.ApiCalls,
                name = IssueCategory.ApiCalls.ToString(),
                groupByDescription = true,
                showAssemblySelection = true,
                showCritical = true,
                showInvertedCallTree = true,
                showFilenameColumn = true,
                showAssemblyColumn = true
            },
            new AnalysisViewDescriptor
            {
                category = IssueCategory.ProjectSettings,
                name = IssueCategory.ProjectSettings.ToString(),
                groupByDescription = false,
                showAssemblySelection = false,
                showCritical = false,
                showInvertedCallTree = false,
                showFilenameColumn = false,
                showAssemblyColumn = false
            }
        };

        private string[] m_ModeNames;
        private ProjectAuditor m_ProjectAuditor;
        private bool m_ShouldRefresh = false;

        // UI
        private readonly List<AnalysisView> m_AnalysisViews = new List<AnalysisView>();
        private TreeViewSelection m_AreaSelection;
        private TreeViewSelection m_AssemblySelection;
        private CallHierarchyView m_CallHierarchyView;
        private CallTreeNode m_CurrentCallTree;
        private SearchField m_SearchField;

        // Serialized fields
        [SerializeField] private int m_ActiveModeIndex;
        [SerializeField] private string m_AreaSelectionSummary;
        [SerializeField] private string[] m_AssemblyNames;
        [SerializeField] private string m_AssemblySelectionSummary;
        [SerializeField] private bool m_DeveloperMode;
        [SerializeField] private ProjectReport m_ProjectReport;
        [SerializeField] private string m_SearchText;
        [SerializeField] private bool m_ShowCallTree;
        [SerializeField] private bool m_ShowDetails = true;
        [SerializeField] private bool m_ShowRecommendation = true;
        [SerializeField] AnalysisState m_AnalysisState = AnalysisState.NotStarted;

        private AnalysisView m_ActiveAnalysisView
        {
            get { return m_AnalysisViews[m_ActiveModeIndex]; }
        }

        private IssueTable m_ActiveIssueTable
        {
            get { return m_ActiveAnalysisView.m_Table; }
        }

        public void AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(Styles.DeveloperMode, m_DeveloperMode, OnToggleDeveloperMode);
            menu.AddItem(Styles.UserMode, !m_DeveloperMode, OnToggleDeveloperMode);
        }

        public bool Match(ProjectIssue issue)
        {
            if (m_ActiveAnalysisView.desc.showAssemblySelection &&
                m_AssemblySelection != null &&
                !m_AssemblySelection.Contains(issue.assembly) &&
                !m_AssemblySelection.ContainsGroup("All"))
                return false;

            if (!m_AreaSelection.Contains(issue.descriptor.area) &&
                !m_AreaSelection.ContainsGroup("All"))
                return false;

            if (!m_ProjectAuditor.config.displayMutedIssues)
                if (m_ProjectAuditor.config.GetAction(issue.descriptor, issue.callingMethod) == Rule.Action.None)
                    return false;

            if (m_ActiveAnalysisView.desc.showCritical &&
                m_ProjectAuditor.config.displayOnlyCriticalIssues &&
                !issue.isPerfCriticalContext)
                return false;

            if (!string.IsNullOrEmpty(m_SearchText))
                if (!MatchesSearch(issue.description) &&
                    !MatchesSearch(issue.filename) &&
                    !MatchesSearch(issue.name))
                    return false;
            return true;
        }

        private void OnEnable()
        {
            m_ProjectAuditor = new ProjectAuditor();

            UpdateAssemblySelection();

            if (m_AreaSelection == null)
            {
                m_AreaSelection = new TreeViewSelection();
                if (!string.IsNullOrEmpty(m_AreaSelectionSummary))
                {
                    if (m_AreaSelectionSummary == "All")
                    {
                        m_AreaSelection.SetAll(m_AreaNames);
                    }
                    else if (m_AreaSelectionSummary != "None")
                    {
                        var areas = m_AreaSelectionSummary.Split(new[] {", "}, StringSplitOptions.None);
                        foreach (var area in areas) m_AreaSelection.selection.Add(area);
                    }
                }
                else
                {
                    m_AreaSelection.SetAll(m_AreaNames);
                }
            }

            m_ModeNames = m_AnalysisViewDescriptors.Select(m => m.name).ToArray();

            m_AnalysisViews.Clear();
            foreach (var desc in m_AnalysisViewDescriptors)
            {
                var view = new AnalysisView(desc, m_ProjectAuditor.config, this);
                view.CreateTable();

                if (m_AnalysisState == AnalysisState.Valid)
                    view.AddIssues(m_ProjectReport.GetIssues(view.desc.category));

                m_AnalysisViews.Add(view);
            }

            m_CallHierarchyView = new CallHierarchyView(new TreeViewState());

            RefreshDisplay();
        }

        private void OnGUI()
        {
            DrawSettings();
            DrawToolbar();
            DrawHelpbox();
            DrawFilters();
            DrawIssues(); // and right-end panels
        }

        private void OnToggleDeveloperMode()
        {
            m_DeveloperMode = !m_DeveloperMode;
        }

        private bool IsAnalysisValid()
        {
            return m_AnalysisState != AnalysisState.NotStarted;
        }

        private bool MatchesSearch(string field)
        {
            return !string.IsNullOrEmpty(field) &&
                field.IndexOf(m_SearchText, StringComparison.CurrentCultureIgnoreCase) >= 0;
        }

        private void Analyze()
        {
            m_ShouldRefresh = true;
            m_AnalysisState = AnalysisState.InProgress;
            m_ProjectReport = new ProjectReport();
            foreach (var view in m_AnalysisViews)
            {
                view.m_Table.Reset();
            }

            var newIssues = new List<ProjectIssue>();

            try
            {
                m_ProjectAuditor.Audit((projectIssue) =>
                {
                    newIssues.Add(projectIssue);
                    m_ProjectReport.AddIssue(projectIssue);
                },
                    (bool completed) =>
                    {
                        // add batch of issues
                        foreach (var view in m_AnalysisViews)
                        {
                            view.AddIssues(newIssues);
                        }
                        newIssues.Clear();

                        if (completed)
                            m_AnalysisState = AnalysisState.Completed;
                        m_ShouldRefresh = true;
                    },
                    new ProgressBarDisplay());
            }
            catch (AssemblyCompilationException e)
            {
                Debug.LogError(e);
            }
        }

        private void RefreshDisplay()
        {
            if (!IsAnalysisValid())
                return;

            if (m_AnalysisState == AnalysisState.Completed)
            {
                // update list of assembly names
                var scriptIssues = m_ProjectReport.GetIssues(IssueCategory.ApiCalls);
                m_AssemblyNames = scriptIssues.Select(i => i.assembly).Distinct().OrderBy(str => str).ToArray();
                UpdateAssemblySelection();

                m_AnalysisState = AnalysisState.Valid;
            }

            m_ActiveIssueTable.Reload();
        }

        private void Reload()
        {
            m_ProjectAuditor.LoadDatabase();
        }

        private void Export()
        {
            if (IsAnalysisValid())
            {
                var path = EditorUtility.SaveFilePanel("Save analysis CSV data", "", "project-auditor-report.csv",
                    "csv");
                if (path.Length != 0) m_ProjectReport.Export(path);
            }
        }

        private void DrawIssues()
        {
            if (!IsAnalysisValid())
                return;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();

            m_ActiveAnalysisView.OnGUI(m_ProjectReport);

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
            var selectedDescriptors = selectedItems.Select(i => i.ProblemDescriptor);
            var selectedIssues = selectedItems.Select(i => i.ProjectIssue);
            // find out if all descriptors are the same
            var firstDescriptor = selectedDescriptors.FirstOrDefault();
            if (selectedDescriptors.Count() == selectedDescriptors.Count(d => d.id == firstDescriptor.id))
                problemDescriptor = firstDescriptor;

            DrawDetailsFoldout(problemDescriptor);
            DrawRecommendationFoldout(problemDescriptor);
            if (m_ActiveAnalysisView.desc.showInvertedCallTree)
            {
                CallTreeNode callTree = null;
                if (selectedIssues.Count() == 1)
                {
                    var issue = selectedIssues.First();
                    if (issue != null)
                        // get caller sub-tree
                        callTree = issue.callTree.GetChild();
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
            var foldoutStyle = new GUIStyle(EditorStyles.foldout);
            foldoutStyle.fontStyle = FontStyle.Bold;
            return EditorGUILayout.Foldout(toggle, content, foldoutStyle);
        }

        private void DrawDetailsFoldout(ProblemDescriptor problemDescriptor)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(LayoutSize.FoldoutWidth),
                GUILayout.MinHeight(LayoutSize.FoldoutMinHeight));

            m_ShowDetails = BoldFoldout(m_ShowDetails, Styles.DetailsFoldout);
            if (m_ShowDetails)
            {
                if (problemDescriptor != null)
                {
                    EditorStyles.textField.wordWrap = true;
                    GUILayout.TextArea(problemDescriptor.problem, GUILayout.MaxHeight(LayoutSize.FoldoutMaxHeight));
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
            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(LayoutSize.FoldoutWidth),
                GUILayout.MinHeight(LayoutSize.FoldoutMinHeight));

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
            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(LayoutSize.FoldoutWidth),
                GUILayout.MinHeight(LayoutSize.FoldoutMinHeight * 2));

            m_ShowCallTree = BoldFoldout(m_ShowCallTree, Styles.CallTreeFoldout);
            if (m_ShowCallTree)
            {
                if (callTree != null)
                {
                    var r = EditorGUILayout.GetControlRect(GUILayout.Height(400));

                    m_CallHierarchyView.OnGUI(r);
                }
                else
                {
                    EditorGUILayout.LabelField(NoIssueSelectedText);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private string GetSelectedAssembliesSummary()
        {
            if (m_AssemblyNames != null && m_AssemblyNames.Length > 0)
                return GetSelectedSummary(m_AssemblySelection, m_AssemblyNames);
            return string.Empty;
        }

        private string GetSelectedAreasSummary()
        {
            return GetSelectedSummary(m_AreaSelection, m_AreaNames);
        }

        // SteveM TODO - This seems wildly more complex than it needs to be... UNLESS assemblies can have sub-assemblies?
        // If that's the case, we need to test for that. Otherwise we need to strip a bunch of this complexity out.
        private string GetSelectedSummary(TreeViewSelection selection, string[] names)
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
                    // Selected assembly valid and in the assembly list
                    // and also within the range of valid assemblies for this data set
                    selectionDict[identifier.name]++;
            }

            // Count all groups where we have 'selected all the items'
            var selectedCount = 0;
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
            var individualItems = new List<string>();
            foreach (var name in selectionDict.Keys)
            {
                var selectionCount = selectionDict[name];
                if (selectionCount <= 0)
                    continue;
                var itemCount = dict[name];
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

            return string.Join(", ", individualItems.ToArray());
        }

        private int CompareUINames(string a, string b)
        {
            var aTokens = a.Split(':');
            var bTokens = b.Split(':');

            if (aTokens.Length > 1 && bTokens.Length > 1)
            {
                var firstName = aTokens[0].Trim();
                var secondName = bTokens[0].Trim();

                if (firstName == secondName)
                {
                    var firstNameIndex = aTokens[1].Trim();
                    var secondNameIndex = bTokens[1].Trim();

                    if (firstNameIndex == "All" && secondNameIndex != "All")
                        return -1;
                    if (firstNameIndex != "All" && secondNameIndex == "All")
                        return 1;

                    int aGroupIndex;
                    if (int.TryParse(firstNameIndex, out aGroupIndex))
                    {
                        int bGroupIndex;
                        if (int.TryParse(secondNameIndex, out bGroupIndex)) return aGroupIndex.CompareTo(bGroupIndex);
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
            var textStyle = GUI.skin.label;
#endif

            var content = new GUIContent(text, text);
            var size = textStyle.CalcSize(content);
            var rect = EditorGUILayout.GetControlRect(GUILayout.MaxWidth(size.x), GUILayout.Height(size.y));
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
            EditorGUILayout.LabelField(Styles.AssemblyFilter, GUILayout.Width(LayoutSize.FilterOptionsLeftLabelWidth));

            var lastEnabled = GUI.enabled;

            GUI.enabled = m_AnalysisState == AnalysisState.Valid && !AssemblySelectionWindow.IsOpen() && m_ActiveAnalysisView.desc.showAssemblySelection;
            if (GUILayout.Button(Styles.AssemblyFilterSelect, EditorStyles.miniButton,
                GUILayout.Width(LayoutSize.FilterOptionsEnumWidth)))
            {
                if (m_AssemblyNames != null && m_AssemblyNames.Length > 0)
                {
                    // Note: Window auto closes as it loses focus so this isn't strictly required
                    if (AssemblySelectionWindow.IsOpen())
                    {
                        AssemblySelectionWindow.CloseAll();
                    }
                    else
                    {
                        var windowPosition =
                            new Vector2(Event.current.mousePosition.x + LayoutSize.FilterOptionsEnumWidth,
                                Event.current.mousePosition.y + GUI.skin.label.lineHeight);
                        var screenPosition = GUIUtility.GUIToScreenPoint(windowPosition);

                        AssemblySelectionWindow.Open(screenPosition.x, screenPosition.y, this, m_AssemblySelection,
                            m_AssemblyNames);
                    }
                }
            }
            GUI.enabled = lastEnabled;

            m_AssemblySelectionSummary = GetSelectedAssembliesSummary();
            DrawSelectedText(m_AssemblySelectionSummary);

            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();
        }

        // SteveM TODO - if AssemblySelectionWindow and AreaSelectionWindow end up sharing a common base class then
        // DrawAssemblyFilter() and DrawAreaFilter() can be made to call a common method and just pass the selection, names
        // and the type of window we want.
        private void DrawAreaFilter()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(Styles.AreaFilter, GUILayout.Width(LayoutSize.FilterOptionsLeftLabelWidth));

            if (m_AreaNames.Length > 0)
            {
                var lastEnabled = GUI.enabled;
                // SteveM TODO - We don't currently have any sense of when the Auditor is busy and should disallow user input
                var enabled = /*!IsAnalysisRunning() &&*/ !AreaSelectionWindow.IsOpen();
                GUI.enabled = enabled;
                if (GUILayout.Button(Styles.AreaFilterSelect, EditorStyles.miniButton,
                    GUILayout.Width(LayoutSize.FilterOptionsEnumWidth)))
                {
                    // Note: Window auto closes as it loses focus so this isn't strictly required
                    if (AreaSelectionWindow.IsOpen())
                    {
                        AreaSelectionWindow.CloseAll();
                    }
                    else
                    {
                        var windowPosition =
                            new Vector2(Event.current.mousePosition.x + LayoutSize.FilterOptionsEnumWidth,
                                Event.current.mousePosition.y + GUI.skin.label.lineHeight);
                        var screenPosition = GUIUtility.GUIToScreenPoint(windowPosition);

                        AreaSelectionWindow.Open(screenPosition.x, screenPosition.y, this, m_AreaSelection,
                            m_AreaNames);
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

            EditorGUILayout.BeginVertical(
                GUI.skin.box /*, GUILayout.Width(LayoutSize.ToolbarWidth), GUILayout.ExpandWidth(true)*/);

            {
                EditorGUILayout.BeginHorizontal();

                var activeModeIndex = GUILayout.Toolbar(m_ActiveModeIndex, m_ModeNames,
                    GUILayout.MaxWidth(LayoutSize.ModeTabWidth) /*, GUILayout.ExpandWidth(true)*/);

                EditorGUILayout.EndHorizontal();

                DrawAssemblyFilter();
                DrawAreaFilter();

                EditorGUI.BeginChangeCheck();

                var searchRect =
                    GUILayoutUtility.GetRect(1, 1, 18, 18, GUILayout.ExpandWidth(true), GUILayout.Width(200));
                EditorGUILayout.BeginHorizontal();

                if (m_SearchField == null) m_SearchField = new SearchField();

                m_SearchText = m_SearchField.OnGUI(searchRect, m_SearchText);

                m_ActiveIssueTable.searchString = m_SearchText;

                EditorGUILayout.EndHorizontal();

                var shouldRefresh = false;

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Selected :", GUILayout.ExpandWidth(true), GUILayout.Width(80));
                if (GUILayout.Button(Styles.MuteButton, GUILayout.ExpandWidth(true), GUILayout.Width(100)))
                {
                    var selectedItems = m_ActiveIssueTable.GetSelectedItems();
                    foreach (var item in selectedItems) SetRuleForItem(item, Rule.Action.None);

                    if (!m_ProjectAuditor.config.displayMutedIssues) m_ActiveIssueTable.SetSelection(new List<int>());
                }

                if (GUILayout.Button(Styles.UnmuteButton, GUILayout.ExpandWidth(true), GUILayout.Width(100)))
                {
                    var selectedItems = m_ActiveIssueTable.GetSelectedItems();
                    foreach (var item in selectedItems) ClearRulesForItem(item);
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Show :", GUILayout.ExpandWidth(true), GUILayout.Width(80));

                GUI.enabled = m_ActiveAnalysisView.desc.showCritical;
                m_ProjectAuditor.config.displayOnlyCriticalIssues = EditorGUILayout.ToggleLeft("Only Critical Issues",
                    m_ProjectAuditor.config.displayOnlyCriticalIssues, GUILayout.Width(160));
                GUI.enabled = true;

                m_ProjectAuditor.config.displayMutedIssues = EditorGUILayout.ToggleLeft("Muted Issues",
                    m_ProjectAuditor.config.displayMutedIssues, GUILayout.Width(127));
                EditorGUILayout.EndHorizontal();

                if (EditorGUI.EndChangeCheck()) shouldRefresh = true;

                if (shouldRefresh || m_ShouldRefresh || m_AnalysisState == AnalysisState.Completed || m_ActiveModeIndex != activeModeIndex)
                {
                    m_ActiveModeIndex = activeModeIndex;
                    RefreshDisplay();
                    m_ShouldRefresh = false;
                }
            }
            EditorGUILayout.EndVertical();
        }

        public void SetAssemblySelection(TreeViewSelection selection)
        {
            m_AssemblySelection = selection;
            RefreshDisplay();
        }

        private void UpdateAssemblySelection()
        {
            if (m_AssemblyNames == null)
                return;

            if (m_AssemblySelection == null) m_AssemblySelection = new TreeViewSelection();

            m_AssemblySelection.selection.Clear();
            if (!string.IsNullOrEmpty(m_AssemblySelectionSummary))
            {
                if (m_AssemblySelectionSummary == "All")
                {
                    m_AssemblySelection.SetAll(m_AssemblyNames);
                }
                else if (m_AssemblySelectionSummary != "None")
                {
                    var assemblies = m_AssemblySelectionSummary.Split(new[] {", "}, StringSplitOptions.None)
                        .Where(assemblyName => m_AssemblyNames.Contains(assemblyName));
                    if (assemblies.Count() > 0)
                        foreach (var assembly in assemblies)
                            m_AssemblySelection.selection.Add(assembly);
                }
            }

            if (!m_AssemblySelection.selection.Any())
            {
                // initial selection setup:
                // - assemblies from user scripts or editable packages, or
                // - default assembly, or,
                // - all generated assemblies

                var compiledAssemblies = m_AssemblyNames.Where(a => !AssemblyHelper.IsModuleAssembly(a));
                if (AssemblyHelper.IsPackageInfoAvailable())
                    compiledAssemblies = compiledAssemblies.Where(a =>
                        !AssemblyHelper.IsAssemblyFromReadOnlyPackage(a));
                m_AssemblySelection.selection.AddRange(compiledAssemblies);

                if (!m_AssemblySelection.selection.Any())
                {
                    if (m_AssemblyNames.Contains(AssemblyHelper.DefaultAssemblyName))
                        m_AssemblySelection.Set(AssemblyHelper.DefaultAssemblyName);
                    else
                        m_AssemblySelection.SetAll(m_AssemblyNames);
                }
            }

            // update assembly selection summary
            m_AssemblySelectionSummary = GetSelectedAssembliesSummary();
        }

        public void SetAreaSelection(TreeViewSelection selection)
        {
            m_AreaSelection = selection;
            RefreshDisplay();
        }

        private void SetRuleForItem(IssueTableItem item, Rule.Action ruleAction)
        {
            var descriptor = item.ProblemDescriptor;

            var callingMethod = "";
            Rule rule;
            if (item.hasChildren)
            {
                rule = m_ProjectAuditor.config.GetRule(descriptor);
            }
            else
            {
                callingMethod = item.ProjectIssue.callingMethod;
                rule = m_ProjectAuditor.config.GetRule(descriptor, callingMethod);
            }

            if (rule == null)
                m_ProjectAuditor.config.AddRule(new Rule
                {
                    id = descriptor.id,
                    filter = callingMethod,
                    action = ruleAction
                });
            else
                rule.action = ruleAction;
        }

        private void ClearRulesForItem(IssueTableItem item)
        {
            m_ProjectAuditor.config.ClearRules(item.ProblemDescriptor,
                item.hasChildren ? string.Empty : item.ProjectIssue.callingMethod);
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(GUI.skin.box);
            {
                GUI.enabled = (m_AnalysisState == AnalysisState.Valid || m_AnalysisState == AnalysisState.NotStarted);

                if (GUILayout.Button(Styles.AnalyzeButton, GUILayout.ExpandWidth(true), GUILayout.Width(80)))
                    Analyze();

                GUI.enabled = m_AnalysisState == AnalysisState.Valid;

                if (GUILayout.Button(Styles.ExportButton, GUILayout.ExpandWidth(true), GUILayout.Width(80)))
                    Export();

                GUI.enabled = true;

                if (m_DeveloperMode)
                    if (GUILayout.Button(Styles.ReloadButton, GUILayout.ExpandWidth(true), GUILayout.Width(80)))
                        Reload();
                if (m_AnalysisState == AnalysisState.InProgress)
                {
                    GUILayout.Label(Styles.AnalysisInProgressLabel, GUILayout.ExpandWidth(true));
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawHelpbox()
        {
            if (!IsAnalysisValid())
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);

                var helpStyle = new GUIStyle(EditorStyles.textField);
                helpStyle.wordWrap = true;

                EditorGUILayout.LabelField(Styles.HelpText, helpStyle);

                EditorGUILayout.EndVertical();
            }
        }

        private void DrawSettings()
        {
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
        }

#if UNITY_2018_1_OR_NEWER
        [MenuItem("Window/Analysis/Project Auditor")]
#else
        [MenuItem("Window/Project Auditor")]
#endif
        public static ProjectAuditorWindow ShowWindow()
        {
            var wnd = GetWindow(typeof(ProjectAuditorWindow)) as ProjectAuditorWindow;
            if (wnd != null) wnd.titleContent = Styles.WindowTitle;
            return wnd;
        }

        // UI styles and layout
        private static class LayoutSize
        {
            public static readonly int ToolbarWidth = 600;
            public static readonly int FoldoutWidth = 300;
            public static readonly int FoldoutMinHeight = 100;
            public static readonly int FoldoutMaxHeight = 220;
            public static readonly int FilterOptionsLeftLabelWidth = 100;
            public static readonly int FilterOptionsEnumWidth = 50;
            public static readonly int ModeTabWidth = 300;
        }

        private static class Styles
        {
            public static readonly GUIContent DeveloperMode = new GUIContent("Developer Mode");
            public static readonly GUIContent UserMode = new GUIContent("User Mode");

            public static readonly GUIContent WindowTitle = new GUIContent("Project Auditor");

            public static readonly GUIContent AnalyzeButton =
                new GUIContent("Analyze", "Analyze Project and list all issues found.");

            public static readonly GUIContent AnalysisInProgressLabel =
                new GUIContent("Analysis in progress...", "Analysis in progress...please wait.");


            public static readonly GUIContent ReloadButton =
                new GUIContent("Reload DB", "Reload Issue Definition files.");

            public static readonly GUIContent ExportButton =
                new GUIContent("Export", "Export project report to .csv files.");

            public static readonly GUIContent AssemblyFilter =
                new GUIContent("Assembly : ", "Select assemblies to examine");

            public static readonly GUIContent AssemblyFilterSelect =
                new GUIContent("Select", "Select assemblies to examine");

            public static readonly GUIContent AreaFilter =
                new GUIContent("Area : ", "Select performance areas to display");

            public static readonly GUIContent AreaFilterSelect =
                new GUIContent("Select", "Select performance areas to display");

            public static readonly GUIContent MuteButton = new GUIContent("Mute", "Always ignore selected issues.");
            public static readonly GUIContent UnmuteButton = new GUIContent("Unmute", "Always show selected issues.");

            public static readonly GUIContent DetailsFoldout = new GUIContent("Details", "Issue Details");

            public static readonly GUIContent RecommendationFoldout =
                new GUIContent("Recommendation", "Recommendation on how to solve the issue");

            public static readonly GUIContent CallTreeFoldout =
                new GUIContent("Inverted Call Hierarchy", "Inverted Call Hierarchy");

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
    }
}
