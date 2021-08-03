using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.ProjectAuditor.Editor.UI.Framework;
using Unity.ProjectAuditor.Editor.Auditors;
using Unity.ProjectAuditor.Editor.CodeAnalysis;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.Profiling;

namespace Unity.ProjectAuditor.Editor.UI
{
    public class ProjectAuditorWindow : EditorWindow, IHasCustomMenu, IProjectIssueFilter
    {
        enum AnalysisState
        {
            Initializing,
            Initialized,
            InProgress,
            Completed,
            Valid
        }

        static readonly string[] AreaNames = Enum.GetNames(typeof(Area));
        static ProjectAuditorWindow m_Instance;

        public static ProjectAuditorWindow Instance
        {
            get
            {
                if (m_Instance == null)
                    ShowWindow();
                return m_Instance;
            }
        }

        Utility.DropdownItem[] m_ViewDropdownItems;
        ProjectAuditor m_ProjectAuditor;
        bool m_ShouldRefresh;
        ProjectAuditorAnalytics.Analytic m_AnalyzeButtonAnalytic;
        ProjectAuditorAnalytics.Analytic m_LoadButtonAnalytic;
        string m_SaveLoadDirectory;

        // UI
        TreeViewSelection m_AreaSelection;
        TreeViewSelection m_AssemblySelection;

        // Serialized fields
        [SerializeField] string m_AreaSelectionSummary;
        [SerializeField] string[] m_AssemblyNames;
        [SerializeField] string m_AssemblySelectionSummary;
        [SerializeField] ProjectReport m_ProjectReport;
        [SerializeField] AnalysisState m_AnalysisState = AnalysisState.Initializing;
        [SerializeField] Preferences m_Preferences = new Preferences();
        [SerializeField] ViewManager m_ViewManager;

        AnalysisView activeView
        {
            get { return m_ViewManager.GetActiveView(); }
        }

        public void AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(Contents.DeveloperMode, m_Preferences.developerMode, OnToggleDeveloperMode);
        }

        public bool Match(ProjectIssue issue)
        {
            // return false if the issue does not match one of these criteria:
            // - assembly name, if applicable
            // - area
            // - is not muted, if enabled
            // - critical context, if enabled/applicable

            Profiler.BeginSample("MatchAssembly");
            var matchAssembly = !activeView.desc.showAssemblySelection ||
                m_AssemblySelection != null &&
                (m_AssemblySelection.Contains(issue.GetCustomProperty(CodeProperty.Assembly)) ||
                    m_AssemblySelection.ContainsGroup("All"));
            Profiler.EndSample();
            if (!matchAssembly)
                return false;

            Profiler.BeginSample("MatchArea");
            var matchArea = !activeView.desc.showAreaSelection ||
                m_AreaSelection.ContainsAny(issue.descriptor.areas) ||
                m_AreaSelection.ContainsGroup("All");
            Profiler.EndSample();
            if (!matchArea)
                return false;

            if (!m_Preferences.mutedIssues && activeView.desc.showMuteOptions)
            {
                Profiler.BeginSample("IsMuted");
                var muted = m_ProjectAuditor.config.GetAction(issue.descriptor, issue.GetCallingMethod()) ==
                    Rule.Severity.None;
                Profiler.EndSample();
                if (muted)
                    return false;
            }

            if (activeView.desc.showCritical &&
                m_Preferences.onlyCriticalIssues &&
                !issue.isPerfCriticalContext)
                return false;

            return true;
        }

        void OnEnable()
        {
            var currentState = m_AnalysisState;
            m_AnalysisState = AnalysisState.Initializing;

            ProjectAuditorAnalytics.EnableAnalytics();

            m_ProjectAuditor = new ProjectAuditor();

            Profiler.BeginSample("Update Selections");
            UpdateAreaSelection();
            UpdateAssemblySelection();
            Profiler.EndSample();

            var viewDescriptors = ViewDescriptor.GetAll();
            Array.Sort(viewDescriptors, (a, b) => a.menuOrder.CompareTo(b.menuOrder));

            m_ViewDropdownItems = new Utility.DropdownItem[viewDescriptors.Length];
            if (m_ViewManager == null || !m_ViewManager.IsValid())
                m_ViewManager = new ViewManager(viewDescriptors.Select(d => d.category).ToArray());
            m_ViewManager.onViewChanged += i =>
            {
                ProjectAuditorAnalytics.SendEvent((ProjectAuditorAnalytics.UIButton)m_ViewManager.GetView(i).desc.analyticsEvent, ProjectAuditorAnalytics.BeginAnalytic());
            };
            m_ViewManager.onViewExported += () =>
            {
                ProjectAuditorAnalytics.SendEvent(ProjectAuditorAnalytics.UIButton.Export, ProjectAuditorAnalytics.BeginAnalytic());
            };

            Profiler.BeginSample("Views Creation");
            m_ViewManager.Create(m_ProjectAuditor, m_Preferences, this, (desc, isSupported) =>
            {
                var index = Array.IndexOf(viewDescriptors, desc);
                m_ViewDropdownItems[index] = new Utility.DropdownItem
                {
                    Content = new GUIContent(string.IsNullOrEmpty(desc.menuLabel) ? desc.name : desc.menuLabel),
                    SelectionContent = new GUIContent("View: " + desc.name),
                    Enabled = isSupported
                };
            });
            Profiler.EndSample();

            // are we reloading from a valid state?
            if (currentState == AnalysisState.Valid && m_ViewManager.activeViewIndex < viewDescriptors.Length)
            {
                Profiler.BeginSample("Views Update");
                m_ViewManager.AddIssues(m_ProjectReport.GetAllIssues());
                m_AnalysisState = currentState;
                Profiler.EndSample();
            }
            else
            {
                m_ProjectReport = new ProjectReport();
                m_AnalysisState = AnalysisState.Initialized;
            }

            Profiler.BeginSample("Refresh");
            RefreshDisplay();
            Profiler.EndSample();

            m_Instance = this;
        }

        void OnDisable()
        {
            m_ViewManager.SaveSettings();
        }

        void OnGUI()
        {
            if (m_AnalysisState == AnalysisState.Completed)
            {
                // switch to summary view after analysis
                m_ViewManager.ChangeView(IssueCategory.MetaData);
            }

            if (m_AnalysisState != AnalysisState.Initializing)
            {
                DrawSettings();
                DrawToolbar();
            }

            if (IsAnalysisValid())
            {
                activeView.DrawInfo();

                if (activeView.IsValid())
                {
                    DrawFilters();
                    DrawActions();

                    if (m_ShouldRefresh || m_AnalysisState == AnalysisState.Completed)
                    {
                        RefreshDisplay();
                        m_ShouldRefresh = false;
                    }

                    activeView.DrawContent();
                }
            }
            else
            {
                DrawHelpbox();
            }
        }

        [InitializeOnLoadMethod]
        static void OnLoad()
        {
            ViewDescriptor.Register(new ViewDescriptor
            {
                category = IssueCategory.MetaData,
                name = "Summary",
                menuOrder = -1,
                showInfoPanel = true,
                type = typeof(SummaryView),
                analyticsEvent = (int)ProjectAuditorAnalytics.UIButton.Summary
            });
            ViewDescriptor.Register(new ViewDescriptor
            {
                category = IssueCategory.Asset,
                name = "Resources",
                menuLabel = "Assets/Resources",
                menuOrder = 1,
                descriptionWithIcon = true,
                showDependencyView = true,
                showFilters = true,
                showRightPanels = true,
                dependencyViewGuiContent = new GUIContent("Asset Dependencies"),
                onDoubleClick = EditorUtil.FocusOnAssetInProjectWindow,
                analyticsEvent = (int)ProjectAuditorAnalytics.UIButton.Assets
            });
            ViewDescriptor.Register(new ViewDescriptor
            {
                category = IssueCategory.Shader,
                name = "Shaders",
                menuOrder = 2,
                menuLabel = "Assets/Shaders",
                descriptionWithIcon = true,
                showFilters = true,
                onDoubleClick = EditorUtil.FocusOnAssetInProjectWindow,
                onDrawToolbarDataOptions = (viewManager) =>
                {
                    if (GUILayout.Button(Contents.ShaderVariantsButton, EditorStyles.toolbarButton,
                        GUILayout.Width(80)))
                    {
                        viewManager.ChangeView(IssueCategory.ShaderVariant);
                    }
                },
                analyticsEvent = (int)ProjectAuditorAnalytics.UIButton.Shaders
            });
            ViewDescriptor.Register(new ViewDescriptor
            {
                type = typeof(ShaderVariantsView),
                category = IssueCategory.ShaderVariant,
                name = "Variants",
                menuOrder = 3,
                menuLabel = "Assets/Shader Variants",
                groupByDescriptor = true,
                showFilters = true,
                showInfoPanel = true,
                onDoubleClick = EditorUtil.FocusOnAssetInProjectWindow,
                onDrawToolbarDataOptions = (viewManager) =>
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.ExpandWidth(true),
                        GUILayout.Width(100)))
                    {
                        Instance.AnalyzeShaderVariants();
                    }
                    if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.ExpandWidth(true),
                        GUILayout.Width(100)))
                    {
                        Instance.ClearShaderVariants();
                    }
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button(Contents.Shaders, EditorStyles.toolbarButton,
                        GUILayout.Width(80)))
                    {
                        viewManager.ChangeView(IssueCategory.Shader);
                    }
                },
                analyticsEvent = (int)ProjectAuditorAnalytics.UIButton.ShaderVariants
            });
            ViewDescriptor.Register(new ViewDescriptor
            {
                category = IssueCategory.Assembly,
                name = "Assemblies",
                menuLabel = "Experimental/Assemblies",
                menuOrder = 99,
                onDoubleClick = EditorUtil.FocusOnAssetInProjectWindow,
                analyticsEvent = (int)ProjectAuditorAnalytics.UIButton.Assemblies
            });
            ViewDescriptor.Register(new ViewDescriptor
            {
                type = typeof(CodeView),
                category = IssueCategory.Code,
                name = "Code",
                menuLabel = "Code/Diagnostics",
                menuOrder = 0,
                groupByDescriptor = true,
                showActions = true,
                showAreaSelection = true,
                showAssemblySelection = true,
                showCritical = true,
                showDependencyView = true,
                showFilters = true,
                showInfoPanel = true,
                showMuteOptions = true,
                showRightPanels = true,
                dependencyViewGuiContent = new GUIContent("Inverted Call Hierarchy"),
                onDoubleClick = EditorUtil.OpenTextFile,
                onOpenDescriptor = EditorUtil.OpenCodeDescriptor,
                analyticsEvent = (int)ProjectAuditorAnalytics.UIButton.ApiCalls
            });
            ViewDescriptor.Register(new ViewDescriptor
            {
                type = typeof(CompilerMessagesView),
                category = IssueCategory.CodeCompilerMessage,
                name = "Compiler Messages",
                menuOrder = 98,
                menuLabel = "Code/C# Compiler Messages",
                groupByDescriptor = true,
                showFilters = true,
                showInfoPanel = true,
                onDoubleClick = EditorUtil.OpenTextFile,
                onOpenDescriptor = EditorUtil.OpenCompilerMessageDescriptor,
                analyticsEvent = (int)ProjectAuditorAnalytics.UIButton.CodeCompilerMessages
            });
            ViewDescriptor.Register(new ViewDescriptor
            {
                category = IssueCategory.GenericInstance,
                name = "Generics",
                menuLabel = "Code/Generic Types Instantiation",
                menuOrder = 99,
                groupByDescriptor = true,
                showAssemblySelection = true,
                showDependencyView = true,
                showFilters = true,
                dependencyViewGuiContent = new GUIContent("Inverted Call Hierarchy"),
                onDoubleClick = EditorUtil.OpenTextFile,
                analyticsEvent = (int)ProjectAuditorAnalytics.UIButton.Generics
            });
            ViewDescriptor.Register(new ViewDescriptor
            {
                category = IssueCategory.ProjectSetting,
                name = "Settings",
                menuLabel = "Settings/Diagnostics",
                menuOrder = 1,
                showActions = true,
                showAreaSelection = true,
                showFilters = true,
                showMuteOptions = true,
                showRightPanels = true,
                onDoubleClick = EditorUtil.OpenProjectSettings,
                analyticsEvent = (int)ProjectAuditorAnalytics.UIButton.ProjectSettings
            });
            ViewDescriptor.Register(new ViewDescriptor
            {
                type = typeof(BuildReportView),
                category = IssueCategory.BuildStep,
                name = "Build Steps",
                menuLabel = "Build Report/Steps",
                menuOrder = 100,
                showFilters = true,
                showInfoPanel = true,
                onDrawToolbarDataOptions = (viewManager) =>
                {
                    if (GUILayout.Button(Contents.BuildFiles, EditorStyles.toolbarButton,
                        GUILayout.Width(80)))
                    {
                        viewManager.ChangeView(IssueCategory.BuildFile);
                    }
                },
                analyticsEvent = (int)ProjectAuditorAnalytics.UIButton.BuildSteps
            });
            ViewDescriptor.Register(new ViewDescriptor
            {
                type = typeof(BuildReportView),
                category = IssueCategory.BuildFile,
                name = "Build Size",
                menuLabel = "Build Report/Size",
                menuOrder = 101,
                groupByDescriptor = true,
                descriptionWithIcon = true,
                showFilters = true,
                showInfoPanel = true,
                onDoubleClick = EditorUtil.FocusOnAssetInProjectWindow,
                onDrawToolbarDataOptions = (viewManager) =>
                {
                    if (GUILayout.Button(Contents.BuildSteps, EditorStyles.toolbarButton,
                        GUILayout.Width(80)))
                    {
                        viewManager.ChangeView(IssueCategory.BuildStep);
                    }
                },
                analyticsEvent = (int)ProjectAuditorAnalytics.UIButton.BuildFiles
            });
        }

        void OnToggleDeveloperMode()
        {
            m_Preferences.developerMode = !m_Preferences.developerMode;
        }

        bool IsAnalysisValid()
        {
            return m_AnalysisState != AnalysisState.Initializing && m_AnalysisState != AnalysisState.Initialized;
        }

        void Analyze()
        {
            m_AnalyzeButtonAnalytic = ProjectAuditorAnalytics.BeginAnalytic();

            m_ShouldRefresh = true;
            m_AnalysisState = AnalysisState.InProgress;
            m_ProjectReport = new ProjectReport();
            m_ViewManager.Clear();

            var newIssues = new List<ProjectIssue>();

            m_ProjectAuditor.Audit(projectIssue =>
            {
                newIssues.Add(projectIssue);
                m_ProjectReport.AddIssue(projectIssue);
            },
                completed =>
                {
                    // add batch of issues
                    m_ViewManager.AddIssues(newIssues.ToArray());
                    newIssues.Clear();

                    if (completed)
                    {
                        m_AnalysisState = AnalysisState.Completed;
                    }

                    m_ShouldRefresh = true;
                },
                new ProgressBar()
            );
        }

        void Update()
        {
            if (m_ShouldRefresh)
                Repaint();
            if (m_AnalysisState == AnalysisState.InProgress)
                Repaint();
        }

        void OnPostprocessBuild(BuildTarget target)
        {
            IncrementalAudit<BuildReportModule>();
        }

        void IncrementalAudit<T>() where T : ProjectAuditorModule
        {
            if (m_ProjectReport == null)
                m_ProjectReport = new ProjectReport();

            var module = m_ProjectAuditor.GetModule<T>();
            if (!module.IsSupported())
                return;

            var layouts = module.GetLayouts().ToArray();
            foreach (var layout in layouts)
            {
                m_ProjectReport.ClearIssues(layout.category);
            }

            var newIssues = new List<ProjectIssue>();
            module.Audit(issue =>
            {
                newIssues.Add(issue);
                m_ProjectReport.AddIssue(issue);
            },
                () =>
                {
                },
                new ProgressBar()
            );

            // update views
            var views = layouts.Select(l => m_ViewManager.GetView(l.category)).Distinct();
            foreach (var view in views)
            {
                view.Clear();
                view.AddIssues(newIssues);
                view.Refresh();
            }
        }

        public void AnalyzeShaderVariants()
        {
            IncrementalAudit<ShadersModule>();
        }

        public void ClearShaderVariants()
        {
            m_ProjectReport.ClearIssues(IssueCategory.ShaderVariant);

            m_ViewManager.ClearView(IssueCategory.ShaderVariant);

            ShadersModule.ClearBuildData();
        }

        void RefreshDisplay()
        {
            if (!IsAnalysisValid())
                return;

            if (m_AnalysisState == AnalysisState.Completed)
            {
                UpdateAssemblyNames();
                UpdateAssemblySelection();

                m_AnalysisState = AnalysisState.Valid;

                if (m_LoadButtonAnalytic != null)
                    ProjectAuditorAnalytics.SendEvent(ProjectAuditorAnalytics.UIButton.Load, m_LoadButtonAnalytic);
                if (m_AnalyzeButtonAnalytic != null)
                    ProjectAuditorAnalytics.SendEventWithAnalyzeSummary(ProjectAuditorAnalytics.UIButton.Analyze, m_AnalyzeButtonAnalytic, m_ProjectReport);

                // repaint once more to make status wheel disappear
                Repaint();
            }

            activeView.Refresh();
            ProjectAuditorAnalytics.SendEvent((ProjectAuditorAnalytics.UIButton)activeView.desc.analyticsEvent, ProjectAuditorAnalytics.BeginAnalytic());
        }

        string GetSelectedAssembliesSummary()
        {
            if (m_AssemblyNames != null && m_AssemblyNames.Length > 0)
                return Utility.GetTreeViewSelectedSummary(m_AssemblySelection, m_AssemblyNames);
            return string.Empty;
        }

        internal string GetSelectedAreasSummary()
        {
            return Utility.GetTreeViewSelectedSummary(m_AreaSelection, AreaNames);
        }

        void DrawAssemblyFilter()
        {
            if (!activeView.desc.showAssemblySelection)
                return;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(Contents.AssemblyFilter, GUILayout.Width(LayoutSize.FilterOptionsLeftLabelWidth));

            var lastEnabled = GUI.enabled;
            GUI.enabled = IsAnalysisValid() && !AssemblySelectionWindow.IsOpen();
            if (GUILayout.Button(Contents.AssemblyFilterSelect, EditorStyles.miniButton,
                GUILayout.Width(LayoutSize.FilterOptionsEnumWidth)))
            {
                if (m_AssemblyNames != null && m_AssemblyNames.Length > 0)
                {
                    var analytic = ProjectAuditorAnalytics.BeginAnalytic();

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

                    ProjectAuditorAnalytics.SendEvent(ProjectAuditorAnalytics.UIButton.AssemblySelect,
                        analytic);
                }
            }

            GUI.enabled = lastEnabled;

            m_AssemblySelectionSummary = GetSelectedAssembliesSummary();
            Utility.DrawSelectedText(m_AssemblySelectionSummary);

            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();
        }

        // stephenm TODO - if AssemblySelectionWindow and AreaSelectionWindow end up sharing a common base class then
        // DrawAssemblyFilter() and DrawAreaFilter() can be made to call a common method and just pass the selection, names
        // and the type of window we want.
        void DrawAreaFilter()
        {
            if (!activeView.desc.showAreaSelection)
                return;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(Contents.AreaFilter, GUILayout.Width(LayoutSize.FilterOptionsLeftLabelWidth));

            if (AreaNames.Length > 0)
            {
                var lastEnabled = GUI.enabled;
                var enabled = IsAnalysisValid() &&
                    !AreaSelectionWindow.IsOpen();
                GUI.enabled = enabled;
                if (GUILayout.Button(Contents.AreaFilterSelect, EditorStyles.miniButton,
                    GUILayout.Width(LayoutSize.FilterOptionsEnumWidth)))
                {
                    var analytic = ProjectAuditorAnalytics.BeginAnalytic();

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
                            AreaNames);
                    }

                    ProjectAuditorAnalytics.SendEvent(ProjectAuditorAnalytics.UIButton.AreaSelect, analytic);
                }

                GUI.enabled = lastEnabled;

                m_AreaSelectionSummary = GetSelectedAreasSummary();
                Utility.DrawSelectedText(m_AreaSelectionSummary);

                GUILayout.FlexibleSpace();
            }

            EditorGUILayout.EndHorizontal();
        }

        void DrawFilters()
        {
            if (!activeView.desc.showFilters)
                return;

            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true));

            m_Preferences.filters = Utility.BoldFoldout(m_Preferences.filters, Contents.FiltersFoldout);
            if (m_Preferences.filters)
            {
                EditorGUI.indentLevel++;

                DrawAssemblyFilter();
                DrawAreaFilter();

                EditorGUI.BeginChangeCheck();

                activeView.DrawTextSearch();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Show :", GUILayout.ExpandWidth(true), GUILayout.Width(80));
                GUI.enabled = activeView.desc.showCritical;

                bool wasShowingCritical = m_Preferences.onlyCriticalIssues;
                m_Preferences.onlyCriticalIssues = EditorGUILayout.ToggleLeft("Only Critical Issues",
                    m_Preferences.onlyCriticalIssues, GUILayout.Width(180));
                GUI.enabled = true;

                if (wasShowingCritical != m_Preferences.onlyCriticalIssues)
                {
                    var analytic = ProjectAuditorAnalytics.BeginAnalytic();
                    var payload = new Dictionary<string, string>();
                    payload["selected"] = activeView.desc.showCritical ? "true" : "false";
                    ProjectAuditorAnalytics.SendEvent(ProjectAuditorAnalytics.UIButton.OnlyCriticalIssues,
                        analytic);
                }

                GUI.enabled = activeView.desc.showMuteOptions;
                bool wasDisplayingMuted = m_Preferences.mutedIssues;
                m_Preferences.mutedIssues = EditorGUILayout.ToggleLeft("Muted Issues",
                    m_Preferences.mutedIssues, GUILayout.Width(127));

                if (wasDisplayingMuted != m_Preferences.mutedIssues)
                {
                    var analytic = ProjectAuditorAnalytics.BeginAnalytic();
                    var payload = new Dictionary<string, string>();
                    payload["selected"] = m_Preferences.mutedIssues ? "true" : "false";
                    ProjectAuditorAnalytics.SendEventWithKeyValues(ProjectAuditorAnalytics.UIButton.ShowMuted,
                        analytic, payload);
                }

                GUI.enabled = true;

                EditorGUILayout.EndHorizontal();

                if (EditorGUI.EndChangeCheck())
                    m_ShouldRefresh = true;

                activeView.DrawFilters();

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();
        }

        void DrawActions()
        {
            if (!activeView.desc.showActions)
                return;

            var table = activeView.table;

            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true));

            m_Preferences.actions = Utility.BoldFoldout(m_Preferences.actions, Contents.ActionsFoldout);
            if (m_Preferences.actions)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.BeginHorizontal();

                GUI.enabled = activeView.desc.showMuteOptions;
                EditorGUILayout.LabelField("Selected :", GUILayout.ExpandWidth(true), GUILayout.Width(80));

                if (GUILayout.Button(Contents.MuteButton, GUILayout.ExpandWidth(true), GUILayout.Width(100)))
                {
                    var analytic = ProjectAuditorAnalytics.BeginAnalytic();
                    var selectedItems = table.GetSelectedItems();
                    foreach (var item in selectedItems)
                    {
                        SetRuleForItem(item, Rule.Severity.None);
                    }

                    if (!m_Preferences.mutedIssues)
                    {
                        table.SetSelection(new List<int>());
                    }

                    ProjectAuditorAnalytics.SendEventWithSelectionSummary(ProjectAuditorAnalytics.UIButton.Mute,
                        analytic, table.GetSelectedItems());
                }

                if (GUILayout.Button(Contents.UnmuteButton, GUILayout.ExpandWidth(true), GUILayout.Width(100)))
                {
                    var analytic = ProjectAuditorAnalytics.BeginAnalytic();
                    var selectedItems = table.GetSelectedItems();
                    foreach (var item in selectedItems)
                    {
                        ClearRulesForItem(item);
                    }

                    ProjectAuditorAnalytics.SendEventWithSelectionSummary(
                        ProjectAuditorAnalytics.UIButton.Unmute, analytic, table.GetSelectedItems());
                }

                GUI.enabled = true;

                EditorGUILayout.EndHorizontal();

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();
        }

        internal void SetAreaSelection(TreeViewSelection selection)
        {
            m_AreaSelection = selection;
            RefreshDisplay();
        }

        internal void SetAssemblySelection(TreeViewSelection selection)
        {
            m_AssemblySelection = selection;
            RefreshDisplay();
        }

        void UpdateAreaSelection()
        {
            if (m_AreaSelection == null)
            {
                m_AreaSelection = new TreeViewSelection();
                if (!string.IsNullOrEmpty(m_AreaSelectionSummary))
                {
                    if (m_AreaSelectionSummary == "All")
                    {
                        m_AreaSelection.SetAll(AreaNames);
                    }
                    else if (m_AreaSelectionSummary != "None")
                    {
                        var areas = m_AreaSelectionSummary.Split(new[] {", "}, StringSplitOptions.None);
                        foreach (var area in areas)
                            m_AreaSelection.selection.Add(area);
                    }
                }
                else
                {
                    m_AreaSelection.SetAll(AreaNames);
                }
            }
        }

        void UpdateAssemblyNames()
        {
            // update list of assembly names
            var scriptIssues = m_ProjectReport.GetIssues(IssueCategory.Code);
            m_AssemblyNames = scriptIssues.Select(i => i.GetCustomProperty(CodeProperty.Assembly)).Distinct().OrderBy(str => str).ToArray();
        }

        void UpdateAssemblySelection()
        {
            if (m_AssemblyNames == null)
                return;

            if (m_AssemblySelection == null)
                m_AssemblySelection = new TreeViewSelection();

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

                var compiledAssemblies = m_AssemblyNames.Where(a => !AssemblyInfoProvider.IsUnityEngineAssembly(a));
                compiledAssemblies = compiledAssemblies.Where(a =>
                    !AssemblyInfoProvider.IsReadOnlyAssembly(a)).ToArray();
                m_AssemblySelection.selection.AddRange(compiledAssemblies);

                if (!m_AssemblySelection.selection.Any())
                {
                    if (m_AssemblyNames.Contains(AssemblyInfo.DefaultAssemblyName))
                        m_AssemblySelection.Set(AssemblyInfo.DefaultAssemblyName);
                    else
                        m_AssemblySelection.SetAll(m_AssemblyNames);
                }
            }

            // update assembly selection summary
            m_AssemblySelectionSummary = GetSelectedAssembliesSummary();
        }

        void SetRuleForItem(IssueTableItem item, Rule.Severity ruleSeverity)
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
                callingMethod = item.ProjectIssue.GetCallingMethod();
                rule = m_ProjectAuditor.config.GetRule(descriptor, callingMethod);
            }

            if (rule == null)
                m_ProjectAuditor.config.AddRule(new Rule
                {
                    id = descriptor.id,
                    filter = callingMethod,
                    severity = ruleSeverity
                });
            else
                rule.severity = ruleSeverity;
        }

        void ClearRulesForItem(IssueTableItem item)
        {
            m_ProjectAuditor.config.ClearRules(item.ProblemDescriptor,
                item.hasChildren ? string.Empty : item.ProjectIssue.GetCallingMethod());
        }

        void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                GUI.enabled = (m_AnalysisState == AnalysisState.Valid || m_AnalysisState == AnalysisState.Initialized);

                const int buttonWidth = 130;
                if (GUILayout.Button(Contents.AnalyzeButton, EditorStyles.toolbarButton, GUILayout.Width(buttonWidth)))
                {
                    Analyze();
                }

                GUI.enabled = m_AnalysisState == AnalysisState.Valid;

                Utility.ToolbarDropdownList(m_ViewDropdownItems,
                    m_ViewManager.activeViewIndex,
                    (obj) => {m_ViewManager.ChangeView((int)obj);}, GUILayout.Width(buttonWidth));

                GUI.enabled = true;

                if (m_AnalysisState == AnalysisState.InProgress)
                {
                    GUILayout.Label(Utility.GetStatusWheel());
                }

                EditorGUILayout.Space();

                const int loadSaveButtonWidth = 40;
                // right-end buttons
                if (GUILayout.Button(Contents.LoadButton, EditorStyles.toolbarButton, GUILayout.Width(loadSaveButtonWidth)))
                {
                    Load();
                }

                GUI.enabled = m_AnalysisState == AnalysisState.Valid;
                if (GUILayout.Button(Contents.SaveButton, EditorStyles.toolbarButton, GUILayout.Width(loadSaveButtonWidth)))
                {
                    Save();
                }
                GUI.enabled = true;

                Utility.DrawHelpButton(Contents.HelpButton, "index");
            }
            EditorGUILayout.EndHorizontal();
        }

        void DrawHelpbox()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);

            EditorGUILayout.LabelField(Contents.HelpText, SharedStyles.TextArea);

            EditorGUILayout.EndVertical();
        }

        void DrawSettings()
        {
            if (m_Preferences.developerMode)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Build :", GUILayout.ExpandWidth(true), GUILayout.Width(80));
                m_ProjectAuditor.config.AnalyzeOnBuild = EditorGUILayout.ToggleLeft("Auto Analyze",
                    m_ProjectAuditor.config.AnalyzeOnBuild, GUILayout.Width(100));
                m_ProjectAuditor.config.FailBuildOnIssues = EditorGUILayout.ToggleLeft("Fail on Issues",
                    m_ProjectAuditor.config.FailBuildOnIssues, GUILayout.Width(100));
                EditorGUILayout.EndHorizontal();
            }
        }

        void Save()
        {
            var path = EditorUtility.SaveFilePanel(k_SaveToFile, m_SaveLoadDirectory, "project-auditor-report.json", "json");
            if (path.Length != 0)
            {
                m_ProjectReport.Save(path);
                m_SaveLoadDirectory = Path.GetDirectoryName(path);

                EditorUtility.RevealInFinder(path);
                ProjectAuditorAnalytics.SendEvent(ProjectAuditorAnalytics.UIButton.Save, ProjectAuditorAnalytics.BeginAnalytic());
            }
        }

        void Load()
        {
            var path = EditorUtility.OpenFilePanel(k_LoadFromFile, m_SaveLoadDirectory, "json");
            if (path.Length != 0)
            {
                m_ProjectReport = ProjectReport.Load(path);
                if (m_ProjectReport.NumTotalIssues == 0)
                {
                    EditorUtility.DisplayDialog(k_LoadFromFile, k_LoadingFailed, "Ok");
                    return;
                }

                m_LoadButtonAnalytic =  ProjectAuditorAnalytics.BeginAnalytic();
                m_AnalysisState = AnalysisState.Valid;
                m_SaveLoadDirectory = Path.GetDirectoryName(path);

                OnEnable();

                UpdateAssemblyNames();
                UpdateAssemblySelection();

                // switch to summary view after loading
                m_ViewManager.ChangeView(IssueCategory.MetaData);
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
                wnd.minSize = new Vector2(LayoutSize.MinWindowWidth, LayoutSize.MinWindowHeight);
                wnd.titleContent = Contents.WindowTitle;
            }

            return wnd;
        }

        const string k_LoadFromFile = "Load from file";
        const string k_LoadingFailed = "Loading report from file was unsuccessful.";
        const string k_SaveToFile = "Save report to json file";

        // UI styles and layout
        static class LayoutSize
        {
            public static readonly int MinWindowWidth = 600;
            public static readonly int MinWindowHeight = 400;
            public static readonly int FilterOptionsLeftLabelWidth = 100;
            public static readonly int FilterOptionsEnumWidth = 50;
        }

        static class Contents
        {
            public static readonly GUIContent DeveloperMode = new GUIContent("Developer Mode");

            public static readonly GUIContent WindowTitle = new GUIContent("Project Auditor");

            public static readonly GUIContent AnalyzeButton =
                new GUIContent("Analyze", "Analyze Project and list all issues found.");

#if UNITY_2019_1_OR_NEWER
            public static readonly GUIContent SaveButton = EditorGUIUtility.TrIconContent("SaveAs", "Save current report to json file");
            public static readonly GUIContent LoadButton = EditorGUIUtility.TrIconContent("Import", "Load report from json file");
#else
            public static readonly GUIContent SaveButton = new GUIContent("Save", "Save current report to json file");
            public static readonly GUIContent LoadButton = new GUIContent("Load", "Load report from json file");
#endif

#if UNITY_2018_1_OR_NEWER
            public static readonly GUIContent HelpButton = EditorGUIUtility.TrIconContent("_Help", "Open Manual (in a web browser)");
#else
            public static readonly GUIContent HelpButton = new GUIContent("?", "Open Manual (in a web browser)");
#endif

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

            public static readonly GUIContent FiltersFoldout = new GUIContent("Filters", "Filtering Criteria");
            public static readonly GUIContent ActionsFoldout = new GUIContent("Actions", "Actions on selected issues");

            public static readonly GUIContent HelpText = new GUIContent(
@"
Project Auditor is an experimental static analysis tool that analyzes assets, settings, and scripts of the Unity project and produces a report that contains the following:

* Code and Settings Diagnostics: a list of possible problems that might affect performance, memory and other areas.
* BuildReport: timing and size information of the last build.
* Assets information

To Analyze the project, click on Analyze.

Once the project is analyzed, Project Auditor displays a summary with high-level information. Then, it is possible to dive into a specific section of the report from the View menu.
A view allows the user to browse through the listed items and filter by string or other search criteria.
"
            );

            public static readonly GUIContent Shaders = new GUIContent("Shaders", "Inspect Shaders");
            public static readonly GUIContent ShaderVariantsButton = new GUIContent("Variants", "Inspect Shader Variants");

            public static readonly GUIContent BuildFiles = new GUIContent("Build Size");
            public static readonly GUIContent BuildSteps = new GUIContent("Build Steps");
        }

        [PostProcessBuild(1)]
        public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
        {
            if (Application.isBatchMode)
                return;

            if (Instance != null)
                Instance.OnPostprocessBuild(target);
        }
    }
}
