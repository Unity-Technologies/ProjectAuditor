using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Unity.ProjectAuditor.Editor.UI.Framework;
using Unity.ProjectAuditor.Editor.Modules;
using Unity.ProjectAuditor.Editor.AssemblyUtils;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Serialization;

namespace Unity.ProjectAuditor.Editor.UI
{
    public class ProjectAuditorWindow : EditorWindow, IProjectIssueFilter
    {
        enum AnalysisState
        {
            Initializing,
            Initialized,
            InProgress,
            Completed,
            Valid
        }

        [Flags]
        enum BuiltInModules
        {
            None = 0,
            Code = 1 << 0,
            Settings = 1 << 1,
            Shaders = 1 << 2,
            Resources = 1 << 3,
            BuildReport = 1 << 4,
            Texture = 1 << 5,

            Everything = ~0
        }


        //JJ add
        ProgressBar progressBar = new ProgressBar();
        int selectIndex;
        //JJ end

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

        GUIContent[] m_PlatformContents;
        BuildTarget[] m_SupportedBuildTargets;
        Utility.DropdownItem[] m_ViewDropdownItems;
        ProjectAuditor m_ProjectAuditor;
        bool m_ShouldRefresh;
        ProjectAuditorAnalytics.Analytic m_AnalyzeButtonAnalytic;
        ProjectAuditorAnalytics.Analytic m_LoadButtonAnalytic;

        // UI
        TreeViewSelection m_AreaSelection;
        TreeViewSelection m_AssemblySelection;

        // Serialized fields
        [SerializeField] BuildTarget m_Platform;
        [SerializeField] BuiltInModules m_SelectedModules = BuiltInModules.Everything;
        [SerializeField] string m_AreaSelectionSummary;
        [SerializeField] string[] m_AssemblyNames;
        [SerializeField] string m_AssemblySelectionSummary;
        [SerializeField] ProjectReport m_ProjectReport;
        [SerializeField] AnalysisState m_AnalysisState = AnalysisState.Initializing;
        [SerializeField] bool m_NewBuildAvailable = false;
        [SerializeField] GlobalStates m_GlobalStates = new GlobalStates();
        [SerializeField] ViewManager m_ViewManager;

        AnalysisView activeView
        {
            get { return m_ViewManager.GetActiveView(); }
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
                (m_AssemblySelection.Contains(activeView.desc.getAssemblyName(issue)) ||
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

            if (!m_GlobalStates.mutedIssues && activeView.desc.showMuteOptions)
            {
                Profiler.BeginSample("IsMuted");
                var muted = m_ProjectAuditor.config.GetAction(issue.descriptor, issue.GetContext()) ==
                    Rule.Severity.None;
                Profiler.EndSample();
                if (muted)
                    return false;
            }

            if (activeView.desc.showCritical &&
                m_GlobalStates.onlyCriticalIssues &&
                !issue.isPerfCriticalContext)
                return false;

            return true;
        }

        void OnEnable()
        {
            var currentState = m_AnalysisState;
            m_AnalysisState = AnalysisState.Initializing;

            var buildTargets = Enum.GetValues(typeof(BuildTarget)).Cast<BuildTarget>();
            var supportedBuildTargets = buildTargets.Where(bt =>
                BuildPipeline.IsBuildTargetSupported(BuildPipeline.GetBuildTargetGroup(bt), bt)).ToList();
            supportedBuildTargets.Sort((t1, t2) => String.Compare(t1.ToString(), t2.ToString(), StringComparison.Ordinal));
            m_SupportedBuildTargets = supportedBuildTargets.ToArray();
            m_PlatformContents = m_SupportedBuildTargets
                .Select(t => new GUIContent(t.ToString())).ToArray();

            if (!BuildPipeline.IsBuildTargetSupported(BuildPipeline.GetBuildTargetGroup(m_Platform), m_Platform))
                m_Platform = EditorUserBuildSettings.activeBuildTarget;

            ProjectAuditorAnalytics.EnableAnalytics();

            Profiler.BeginSample("Update Selections");
            UpdateAreaSelection();
            UpdateAssemblySelection();
            Profiler.EndSample();

            // are we reloading from a valid state?
            if (currentState == AnalysisState.Valid)
            {
                m_ProjectAuditor = new ProjectAuditor();

                var categories = m_ProjectReport.GetAllIssues().Select(i => i.category).Distinct().ToArray();
                InitializeViews(categories, true);

                Profiler.BeginSample("Views Update");
                m_ViewManager.AddIssues(m_ProjectReport.GetAllIssues());
                m_AnalysisState = currentState;
                Profiler.EndSample();
            }
            else
            {
                m_AnalysisState = AnalysisState.Initialized;
            }

            Profiler.BeginSample("Refresh");
            RefreshDisplay();
            Profiler.EndSample();

            m_Instance = this;

            //jj add
            PackagesUtils.Initial(ProjectAuditor.DataPath, "TestPackages.json");
            //jj end
        }

        void InitializeViews(IssueCategory[] categories, bool reload)
        {
            if (m_ViewManager == null || !reload)
            {
                var viewDescriptors = ViewDescriptor.GetAll()
                    .Where(descriptor => categories.Contains(descriptor.category)).ToArray();
                Array.Sort(viewDescriptors, (a, b) => a.menuOrder.CompareTo(b.menuOrder));

                m_ViewManager = new ViewManager(viewDescriptors.Select(d => d.category).ToArray()); // view manager needs sorted categories
            }

            m_ViewManager.onViewChanged += i =>
            {
                ProjectAuditorAnalytics.SendEvent(
                    (ProjectAuditorAnalytics.UIButton)m_ViewManager.GetView(i).desc.analyticsEvent,
                    ProjectAuditorAnalytics.BeginAnalytic());
            };

            m_ViewManager.onAnalyze += IncrementalAudit;
            m_ViewManager.onViewExported += () =>
            {
                ProjectAuditorAnalytics.SendEvent(ProjectAuditorAnalytics.UIButton.Export,
                    ProjectAuditorAnalytics.BeginAnalytic());
            };

            Profiler.BeginSample("Views Creation");

            var dropdownItems = new List<Utility.DropdownItem>(categories.Length);
            m_ViewManager.Create(m_ProjectAuditor, m_GlobalStates, (desc, isSupported) =>
            {
                dropdownItems.Add(new Utility.DropdownItem
                {
                    Content = new GUIContent(string.IsNullOrEmpty(desc.menuLabel) ? desc.name : desc.menuLabel),
                    SelectionContent = new GUIContent("View: " + desc.name),
                    Enabled = isSupported,
                    UserData = desc.category
                });
            }, this);
            m_ViewDropdownItems = dropdownItems.ToArray();

            Profiler.EndSample();
        }

        void OnDisable()
        {
            m_ViewManager?.SaveSettings();
        }

        void OnGUI()
        {
            if (m_AnalysisState == AnalysisState.Completed)
            {
                // switch to summary view after analysis
                m_ViewManager.ChangeView(IssueCategory.MetaData);
            }

            if (m_AnalysisState != AnalysisState.Initializing && m_AnalysisState != AnalysisState.Initialized)
            {
                DrawToolbar();
            }

            if (IsAnalysisValid())
            {
                DrawReport();
            }
            else
            {
                DrawHome();
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
                dependencyViewGuiContent = new GUIContent("Asset Dependencies"),
                onOpenIssue = EditorUtil.FocusOnAssetInProjectWindow,
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
                onContextMenu = (menu, viewManager, issue) =>
                {
                    menu.AddItem(Contents.ShaderVariants, false, () =>
                    {
                        viewManager.ChangeView(IssueCategory.ShaderVariant);
                        viewManager.GetActiveView().SetSearch(issue.description);
                    });
                },
                onOpenIssue = EditorUtil.FocusOnAssetInProjectWindow,
                onDrawToolbar = (viewManager) =>
                {
                    ChangeViewButton(viewManager, IssueCategory.ShaderCompilerMessage, Contents.ShaderCompilerMessages);
                    ChangeViewButton(viewManager, IssueCategory.ShaderVariant, Contents.ShaderVariants);
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
                showFilters = true,
                showInfoPanel = true,
                showRightPanels = true,
                onOpenIssue = EditorUtil.FocusOnAssetInProjectWindow,
                onDrawToolbar = (viewManager) =>
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.ExpandWidth(true),
                        GUILayout.Width(AnalysisView.toolbarButtonSize)))
                    {
                        Instance.AnalyzeShaderVariants();
                    }
                    if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.ExpandWidth(true),
                        GUILayout.Width(AnalysisView.toolbarButtonSize)))
                    {
                        Instance.ClearShaderVariants();
                    }
                    GUILayout.FlexibleSpace();

                    ChangeViewButton(viewManager, IssueCategory.ShaderCompilerMessage, Contents.ShaderCompilerMessages);
                    ChangeViewButton(viewManager, IssueCategory.Shader, Contents.Shaders);
                },
                analyticsEvent = (int)ProjectAuditorAnalytics.UIButton.ShaderVariants
            });

#if UNITY_2019_1_OR_NEWER
            ViewDescriptor.Register(new ViewDescriptor
            {
                category = IssueCategory.ShaderCompilerMessage,
                name = "Shader Messages",
                menuLabel = "Experimental/Shader Compiler Messages",
                menuOrder = 4,
                descriptionWithIcon = true,
                onOpenIssue = EditorUtil.OpenTextFile<Shader>,
                onDrawToolbar = (viewManager) =>
                {
                    ChangeViewButton(viewManager, IssueCategory.Shader, Contents.Shaders);
                    ChangeViewButton(viewManager, IssueCategory.ShaderVariant, Contents.ShaderVariants);
                },
                analyticsEvent = (int)ProjectAuditorAnalytics.UIButton.ShaderCompilerMessages
            });
#endif
            ViewDescriptor.Register(new ViewDescriptor
            {
                category = IssueCategory.Assembly,
                name = "Assemblies",
                menuLabel = "Code/Assemblies",
                menuOrder = 98,
                showAssemblySelection = true,
                showFilters = true,
                showDependencyView = true,
                getAssemblyName = issue => issue.description,
                onOpenIssue = EditorUtil.FocusOnAssetInProjectWindow,
                analyticsEvent = (int)ProjectAuditorAnalytics.UIButton.Assemblies
            });
            ViewDescriptor.Register(new ViewDescriptor
            {
                category = IssueCategory.PrecompiledAssembly,
                name = "Precompiled Assemblies",
                menuLabel = "Experimental/Precompiled Assemblies",
                menuOrder = 91,
                showFilters = true,
                getAssemblyName = issue => issue.description,
                onOpenIssue = EditorUtil.FocusOnAssetInProjectWindow,
                analyticsEvent = (int)ProjectAuditorAnalytics.UIButton.PrecompiledAssemblies
            });
            ViewDescriptor.Register(new ViewDescriptor
            {
                type = typeof(CodeView),
                category = IssueCategory.Code,
                name = "Code",
                menuLabel = "Code/Diagnostics",
                menuOrder = 0,
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
                getAssemblyName = issue => issue.GetCustomProperty(CodeProperty.Assembly),
                onOpenIssue = EditorUtil.OpenTextFile<TextAsset>,
                onOpenManual = EditorUtil.OpenCodeDescriptor,
                analyticsEvent = (int)ProjectAuditorAnalytics.UIButton.ApiCalls
            });
            ViewDescriptor.Register(new ViewDescriptor
            {
                type = typeof(CompilerMessagesView),
                category = IssueCategory.CodeCompilerMessage,
                name = "Compiler Messages",
                menuOrder = 98,
                menuLabel = "Code/C# Compiler Messages",
                //showAssemblySelection = true,
                showFilters = true,
                showSeverityFilters = true,
                showInfoPanel = true,
                //getAssemblyName = issue => issue.GetCustomProperty(CompilerMessageProperty.Assembly),
                onOpenIssue = EditorUtil.OpenTextFile<TextAsset>,
                onOpenManual = EditorUtil.OpenCompilerMessageDescriptor,
                analyticsEvent = (int)ProjectAuditorAnalytics.UIButton.CodeCompilerMessages
            });
            ViewDescriptor.Register(new ViewDescriptor
            {
                category = IssueCategory.GenericInstance,
                name = "Generics",
                menuLabel = "Experimental/Generic Types Instantiation",
                menuOrder = 90,
                showAssemblySelection = true,
                showDependencyView = true,
                showFilters = true,
                dependencyViewGuiContent = new GUIContent("Inverted Call Hierarchy"),
                getAssemblyName = issue => issue.GetCustomProperty(CodeProperty.Assembly),
                onOpenIssue = EditorUtil.OpenTextFile<TextAsset>,
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
                onOpenIssue = EditorUtil.OpenProjectSettings,
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
                onDrawToolbar = (viewManager) =>
                {
                    ChangeViewButton(viewManager, IssueCategory.BuildFile, Contents.BuildFiles);
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
                descriptionWithIcon = true,
                showFilters = true,
                showInfoPanel = true,
                onOpenIssue = EditorUtil.FocusOnAssetInProjectWindow,
                onDrawToolbar = (viewManager) =>
                {
                    ChangeViewButton(viewManager, IssueCategory.BuildStep, Contents.BuildSteps);
                },
                analyticsEvent = (int)ProjectAuditorAnalytics.UIButton.BuildFiles
            });
        }

        static void ChangeViewButton(ViewManager viewManager, IssueCategory category, GUIContent guiContent)
        {
            if (GUILayout.Button(
                guiContent, EditorStyles.toolbarButton,
                GUILayout.Width(AnalysisView.toolbarButtonSize)))
            {
                viewManager.ChangeView(category);
            }
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
            m_ProjectReport = null;

            m_ProjectAuditor = new ProjectAuditor();

            var selectedCategories = GetSelectedCategories();
            InitializeViews(selectedCategories, false);

            var newIssues = new List<ProjectIssue>();
            var projectAuditorParams = new ProjectAuditorParams
            {
                categories = m_SelectedModules == BuiltInModules.Everything ? null : selectedCategories,
                platform = m_Platform,
                onIssueFound = projectIssue =>
                {
                    newIssues.Add(projectIssue);
                },
                onUpdate = projectReport =>
                {
                    // add batch of issues
                    m_ViewManager.AddIssues(newIssues.ToArray());
                    newIssues.Clear();

                    if (projectReport != null)
                    {
                        m_AnalysisState = AnalysisState.Completed;
                        m_ProjectReport = projectReport;
                    }

                    m_ShouldRefresh = true;
                }
            };
            m_ProjectAuditor.AuditAsync(projectAuditorParams, new ProgressBar());
        }

        void Update()
        {
            if (m_ShouldRefresh)
                Repaint();
            if (m_AnalysisState == AnalysisState.InProgress)
                Repaint();
            if (m_NewBuildAvailable && LastBuildReportProvider.GetLastBuildReportAsset() != null)
                m_NewBuildAvailable = false;
        }

        void OnPostprocessBuild(BuildTarget target)
        {
            // Note that we can't run BuildReportModule in OnPostprocessBuild because the Library/LastBuild.buildreport file is only created AFTER OnPostprocessBuild
            if (m_ProjectAuditor != null && m_ProjectAuditor.config.SaveBuildReports)
                m_NewBuildAvailable = true;
        }

        void IncrementalAudit<T>() where T : ProjectAuditorModule
        {
            var module = m_ProjectAuditor.GetModule<T>();
            if (!module.IsSupported())
                return;

            IncrementalAudit(module);
        }

        void IncrementalAudit(ProjectAuditorModule module)
        {
            if (m_ProjectReport == null)
                m_ProjectReport = new ProjectReport();

            var categories = module.GetCategories().ToArray();
            foreach (var category in categories)
            {
                m_ProjectReport.ClearIssues(category);
            }

            var newIssues = new List<ProjectIssue>();
            var projectAuditorParams = new ProjectAuditorParams
            {
                onIssueFound = issue =>
                {
                    newIssues.Add(issue);
                    m_ProjectReport.AddIssue(issue);
                }
            };
            module.Audit(projectAuditorParams, new ProgressBar());

            // update views
            var views = categories.Select(c => m_ViewManager.GetView(c)).Distinct();
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

        IssueCategory[] GetSelectedCategories()
        {
            if (m_SelectedModules == BuiltInModules.Everything)
                return m_ProjectAuditor.GetCategories();

            var requestedCategories = new List<IssueCategory>(new[] {IssueCategory.MetaData});
            if ((m_SelectedModules.HasFlag(BuiltInModules.Code)))
                requestedCategories.AddRange(m_ProjectAuditor.GetModule<CodeModule>().GetCategories());
            if (m_SelectedModules.HasFlag(BuiltInModules.Settings))
                requestedCategories.AddRange(m_ProjectAuditor.GetModule<SettingsModule>().GetCategories());
            if ((m_SelectedModules.HasFlag(BuiltInModules.Shaders)))
                requestedCategories.AddRange(m_ProjectAuditor.GetModule<ShadersModule>().GetCategories());
            if ((m_SelectedModules.HasFlag(BuiltInModules.Resources)))
                requestedCategories.AddRange(m_ProjectAuditor.GetModule<AssetsModule>().GetCategories());
            if ((m_SelectedModules.HasFlag(BuiltInModules.BuildReport)))
                requestedCategories.AddRange(m_ProjectAuditor.GetModule<BuildReportModule>().GetCategories());


            // jj
            if ((m_SelectedModules.HasFlag(BuiltInModules.Texture)))
                requestedCategories.AddRange(m_ProjectAuditor.GetModule<SettingsModule>().GetCategories());
            //end jj

            return requestedCategories.ToArray();
        }

        void DrawAssemblyFilter()
        {
            if (!activeView.desc.showAssemblySelection)
                return;

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(Contents.AssemblyFilter, GUILayout.Width(LayoutSize.FilterOptionsLeftLabelWidth));

                using (new EditorGUI.DisabledScope(!IsAnalysisValid() || SelectionWindow.IsOpen<AssemblySelectionWindow>()))
                {
                    if (GUILayout.Button(Contents.AssemblyFilterSelect, EditorStyles.miniButton,
                        GUILayout.Width(LayoutSize.FilterOptionsEnumWidth)))
                    {
                        if (m_AssemblyNames != null && m_AssemblyNames.Length > 0)
                        {
                            var analytic = ProjectAuditorAnalytics.BeginAnalytic();

                            // Note: Window auto closes as it loses focus so this isn't strictly required
                            if (SelectionWindow.IsOpen<AssemblySelectionWindow>())
                            {
                                SelectionWindow.CloseAll<AssemblySelectionWindow>();
                            }
                            else
                            {
                                var windowPosition =
                                    new Vector2(Event.current.mousePosition.x + LayoutSize.FilterOptionsEnumWidth,
                                        Event.current.mousePosition.y + GUI.skin.label.lineHeight);
                                var screenPosition = GUIUtility.GUIToScreenPoint(windowPosition);

                                SelectionWindow.Open<AssemblySelectionWindow>("Assemblies", screenPosition.x, screenPosition.y, m_AssemblySelection,
                                    m_AssemblyNames, selection =>
                                    {
                                        var selectEvent = ProjectAuditorAnalytics.BeginAnalytic();
                                        SetAssemblySelection(selection);

                                        var payload = new Dictionary<string, string>();
                                        var selectedAsmNames = selection.selection;

                                        payload["numSelected"] = selectedAsmNames.Count.ToString();
                                        payload["numUnityAssemblies"] = selectedAsmNames.Count(assemblyName => assemblyName.Contains("Unity")).ToString();

                                        ProjectAuditorAnalytics.SendEventWithKeyValues(ProjectAuditorAnalytics.UIButton.AssemblySelectApply, selectEvent, payload);
                                    });
                            }

                            ProjectAuditorAnalytics.SendEvent(ProjectAuditorAnalytics.UIButton.AssemblySelect,
                                analytic);
                        }
                    }
                }

                m_AssemblySelectionSummary = GetSelectedAssembliesSummary();
                Utility.DrawSelectedText(m_AssemblySelectionSummary);

                GUILayout.FlexibleSpace();
            }
        }

        // stephenm TODO - if AssemblySelectionWindow and AreaSelectionWindow end up sharing a common base class then
        // DrawAssemblyFilter() and DrawAreaFilter() can be made to call a common method and just pass the selection, names
        // and the type of window we want.
        void DrawAreaFilter()
        {
            if (!activeView.desc.showAreaSelection)
                return;

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(Contents.AreaFilter,
                    GUILayout.Width(LayoutSize.FilterOptionsLeftLabelWidth));

                if (AreaNames.Length > 0)
                {
                    using (new EditorGUI.DisabledScope(!IsAnalysisValid() || SelectionWindow.IsOpen<AreaSelectionWindow>()))
                    {
                        if (GUILayout.Button(Contents.AreaFilterSelect, EditorStyles.miniButton,
                            GUILayout.Width(LayoutSize.FilterOptionsEnumWidth)))
                        {
                            var analytic = ProjectAuditorAnalytics.BeginAnalytic();

                            // Note: Window auto closes as it loses focus so this isn't strictly required
                            if (SelectionWindow.IsOpen<AreaSelectionWindow>())
                            {
                                SelectionWindow.CloseAll<AreaSelectionWindow>();
                            }
                            else
                            {
                                var windowPosition =
                                    new Vector2(Event.current.mousePosition.x + LayoutSize.FilterOptionsEnumWidth,
                                        Event.current.mousePosition.y + GUI.skin.label.lineHeight);
                                var screenPosition = GUIUtility.GUIToScreenPoint(windowPosition);

                                SelectionWindow.Open<AreaSelectionWindow>("Areas", screenPosition.x, screenPosition.y, m_AreaSelection,
                                    AreaNames, selection =>
                                    {
                                        var selectEvent = ProjectAuditorAnalytics.BeginAnalytic();
                                        SetAreaSelection(selection);

                                        var payload = new Dictionary<string, string>();
                                        payload["areas"] = GetSelectedAreasSummary();
                                        ProjectAuditorAnalytics.SendEventWithKeyValues(ProjectAuditorAnalytics.UIButton.AreaSelectApply, selectEvent, payload);
                                    });
                            }

                            ProjectAuditorAnalytics.SendEvent(ProjectAuditorAnalytics.UIButton.AreaSelect, analytic);
                        }
                    }

                    m_AreaSelectionSummary = GetSelectedAreasSummary();
                    Utility.DrawSelectedText(m_AreaSelectionSummary);

                    GUILayout.FlexibleSpace();
                }
            }
        }

        void DrawFilters()
        {
            if (!activeView.desc.showFilters)
                return;

            using (new EditorGUILayout.VerticalScope(GUI.skin.box, GUILayout.ExpandWidth(true)))
            {
                m_GlobalStates.filters = Utility.BoldFoldout(m_GlobalStates.filters, Contents.FiltersFoldout);
                if (m_GlobalStates.filters)
                {
                    EditorGUI.indentLevel++;

                    DrawAssemblyFilter();
                    DrawAreaFilter();

                    EditorGUI.BeginChangeCheck();

                    activeView.DrawTextSearch();

                    // this is specific to diagnostics
                    if (activeView.desc.showCritical || activeView.desc.showMuteOptions)
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.LabelField("Show :", GUILayout.ExpandWidth(true), GUILayout.Width(80));

                            if (activeView.desc.showCritical)
                            {
                                bool wasShowingCritical = m_GlobalStates.onlyCriticalIssues;
                                m_GlobalStates.onlyCriticalIssues = EditorGUILayout.ToggleLeft("Only Critical Issues",
                                    m_GlobalStates.onlyCriticalIssues, GUILayout.Width(180));

                                if (wasShowingCritical != m_GlobalStates.onlyCriticalIssues)
                                {
                                    var analytic = ProjectAuditorAnalytics.BeginAnalytic();
                                    var payload = new Dictionary<string, string>();
                                    payload["selected"] = activeView.desc.showCritical ? "true" : "false";
                                    ProjectAuditorAnalytics.SendEventWithKeyValues(ProjectAuditorAnalytics.UIButton.OnlyCriticalIssues,
                                        analytic, payload);
                                }
                            }

                            if (activeView.desc.showMuteOptions)
                            {
                                bool wasDisplayingMuted = m_GlobalStates.mutedIssues;
                                m_GlobalStates.mutedIssues = EditorGUILayout.ToggleLeft("Muted Issues",
                                    m_GlobalStates.mutedIssues, GUILayout.Width(127));

                                if (wasDisplayingMuted != m_GlobalStates.mutedIssues)
                                {
                                    var analytic = ProjectAuditorAnalytics.BeginAnalytic();
                                    var payload = new Dictionary<string, string>();
                                    payload["selected"] = m_GlobalStates.mutedIssues ? "true" : "false";
                                    ProjectAuditorAnalytics.SendEventWithKeyValues(
                                        ProjectAuditorAnalytics.UIButton.ShowMuted,
                                        analytic, payload);
                                }
                            }
                        }
                    }

                    if (EditorGUI.EndChangeCheck())
                        m_ShouldRefresh = true;

                    activeView.DrawFilters();

                    EditorGUI.indentLevel--;
                }
            }
        }

        void DrawActions()
        {
            if (!activeView.desc.showActions)
                return;

            var table = activeView.table;

            using (new EditorGUILayout.VerticalScope(GUI.skin.box, GUILayout.ExpandWidth(true)))
            {
                m_GlobalStates.actions = Utility.BoldFoldout(m_GlobalStates.actions, Contents.ActionsFoldout);
                if (m_GlobalStates.actions)
                {
                    EditorGUI.indentLevel++;

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("Selected :", GUILayout.ExpandWidth(true), GUILayout.Width(80));

                        using (new EditorGUI.DisabledScope(!activeView.desc.showMuteOptions))
                        {
                            if (GUILayout.Button(Contents.MuteButton, GUILayout.ExpandWidth(true), GUILayout.Width(100)))
                            {
                                var analytic = ProjectAuditorAnalytics.BeginAnalytic();
                                var selectedItems = table.GetSelectedItems();
                                foreach (var item in selectedItems)
                                {
                                    SetRuleForItem(item, Rule.Severity.None);
                                }

                                if (!m_GlobalStates.mutedIssues)
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
                        }
                    }

                    EditorGUI.indentLevel--;
                }
            }
        }

        void DrawHome()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));

            EditorGUILayout.LabelField(Contents.WelcomeText, SharedStyles.TextArea);
            EditorGUILayout.Space();

            m_SelectedModules = (BuiltInModules)EditorGUILayout.EnumFlagsField(Contents.ModulesSelection, m_SelectedModules, GUILayout.ExpandWidth(true));

            var selectedTarget = Array.IndexOf(m_SupportedBuildTargets, m_Platform);
            selectedTarget = EditorGUILayout.Popup(Contents.PlatformSelection, selectedTarget, m_PlatformContents);
            m_Platform = m_SupportedBuildTargets[selectedTarget];

            EditorGUILayout.Space();

            //JJ
            using (new EditorGUILayout.HorizontalScope())
            {
                selectIndex = EditorGUILayout.Popup(Contents.Packages, selectIndex, PackagesUtils.GetPackagesNames());
                const int height = 30;
                using (new EditorGUI.DisabledScope(selectIndex == 0))
                {
                    if (GUILayout.Button(Contents.InstallBtn, GUILayout.Width(100), GUILayout.Height(height)))
                    {
                        PackagesUtils.InstallPackage(selectIndex, progressBar);
                    }
                }
            }
            //end JJ

            GUILayout.FlexibleSpace();

            using (new EditorGUILayout.HorizontalScope())
            {
                const int height = 30;

                GUILayout.FlexibleSpace();

                using (new EditorGUI.DisabledScope(m_SelectedModules == BuiltInModules.None))
                {
                    if (GUILayout.Button(Contents.AnalyzeButton, GUILayout.Width(100), GUILayout.Height(height)))
                    {
                        Analyze();
                    }
                }

                if (GUILayout.Button(Contents.LoadButton, GUILayout.Width(40), GUILayout.Height(height)))
                {
                    Load();
                }
            }

            EditorGUILayout.Space();

            EditorGUILayout.EndVertical();
        }

        void DrawReport()
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
                        var areas = Formatting.SplitStrings(m_AreaSelectionSummary);
                        m_AreaSelection.selection.AddRange(areas);
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
            var assemblyNames = m_ProjectReport.GetIssues(IssueCategory.Assembly).Select(i => i.description).ToArray();
            m_AssemblyNames = assemblyNames.Distinct().OrderBy(str => str).ToArray();
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
                    var assemblies = Formatting.SplitStrings(m_AssemblySelectionSummary)
                        .Where(assemblyName => m_AssemblyNames.Contains(assemblyName));
                    m_AssemblySelection.selection.AddRange(assemblies);
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
            if (item.ProjectIssue == null)
                return;

            var descriptor = item.ProjectIssue.descriptor;
            var context = item.ProjectIssue.GetContext();
            var rule = m_ProjectAuditor.config.GetRule(descriptor, context);

            if (rule == null)
                m_ProjectAuditor.config.AddRule(new Rule
                {
                    id = descriptor.id,
                    filter = context,
                    severity = ruleSeverity
                });
            else
                rule.severity = ruleSeverity;
        }

        void ClearRulesForItem(IssueTableItem item)
        {
            var descriptor = item.ProjectIssue.descriptor;
            m_ProjectAuditor.config.ClearRules(descriptor,
                item.hasChildren ? string.Empty : item.ProjectIssue.GetContext());
        }

        void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                const int largeButtonWidth = 200;

                Utility.ToolbarDropdownList(m_ViewDropdownItems,
                    m_ViewManager.activeViewIndex,
                    (category) => {m_ViewManager.ChangeView((IssueCategory)category);}, GUILayout.Width(largeButtonWidth));

                if (m_AnalysisState == AnalysisState.InProgress)
                {
                    GUILayout.Label(Utility.GetStatusWheel());
                }

                EditorGUILayout.Space();

                // right-end buttons
                using (new EditorGUI.DisabledScope(m_AnalysisState != AnalysisState.Valid))
                {
                    const int loadSaveButtonWidth = 60;
                    if (GUILayout.Button(Contents.LoadButton, EditorStyles.toolbarButton, GUILayout.Width(loadSaveButtonWidth)))
                    {
                        Load();
                    }

                    if (GUILayout.Button(Contents.SaveButton, EditorStyles.toolbarButton, GUILayout.Width(loadSaveButtonWidth)))
                    {
                        Save();
                    }

                    if (GUILayout.Button(Contents.DiscardButton, EditorStyles.toolbarButton, GUILayout.Width(loadSaveButtonWidth)))
                    {
                        if (EditorUtility.DisplayDialog(k_Discard, k_DiscardQuestion, "Ok", "Cancel"))
                        {
                            m_AnalysisState = AnalysisState.Initialized;
                        }
                    }
                }

                Utility.DrawHelpButton(Contents.HelpButton, "index");
            }
        }

        void Save()
        {
            var path = EditorUtility.SaveFilePanel(k_SaveToFile, UserPreferences.loadSavePath, "project-auditor-report.json", "json");
            if (path.Length != 0)
            {
                m_ProjectReport.Save(path);
                UserPreferences.loadSavePath = Path.GetDirectoryName(path);

                EditorUtility.RevealInFinder(path);
                ProjectAuditorAnalytics.SendEvent(ProjectAuditorAnalytics.UIButton.Save, ProjectAuditorAnalytics.BeginAnalytic());
            }
        }

        void Load()
        {
            var path = EditorUtility.OpenFilePanel(k_LoadFromFile, UserPreferences.loadSavePath, "json");
            if (path.Length != 0)
            {
                m_ProjectReport = ProjectReport.Load(path);
                if (m_ProjectReport.NumTotalIssues == 0)
                {
                    EditorUtility.DisplayDialog(k_LoadFromFile, k_LoadingFailed, "Ok");
                    return;
                }

                m_ProjectAuditor = new ProjectAuditor();

                m_LoadButtonAnalytic =  ProjectAuditorAnalytics.BeginAnalytic();
                m_AnalysisState = AnalysisState.Valid;
                UserPreferences.loadSavePath = Path.GetDirectoryName(path);
                m_ViewManager = null; // make sure ViewManager is reinitialized

                OnEnable();

                UpdateAssemblyNames();
                UpdateAssemblySelection();

                // switch to summary view after loading
                m_ViewManager.ChangeView(IssueCategory.MetaData);
            }
        }

        [MenuItem("Window/Analysis/Project Auditor")]
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
        const string k_Discard = "Discard current report";
        const string k_DiscardQuestion = "The current report will be lost. Are you sure?";

        // UI styles and layout
        static class LayoutSize
        {
            public static readonly int MinWindowWidth = 410;
            public static readonly int MinWindowHeight = 340;
            public static readonly int FilterOptionsLeftLabelWidth = 100;
            public static readonly int FilterOptionsEnumWidth = 50;
        }

        static class Contents
        {
            //JJ
            public static readonly GUIContent Packages = new GUIContent("Packages", "Select Package to install");
            public static readonly GUIContent InstallBtn = new GUIContent("Install", "Install the selected Package");
            //end jj


            public static readonly GUIContent WindowTitle = new GUIContent("Project Auditor");

            public static readonly GUIContent AnalyzeButton =
                new GUIContent("Analyze", "Analyze Project and list all issues found.");
            public static readonly GUIContent ModulesSelection =
                new GUIContent("Modules", "Select Project Auditor modules.");
            public static readonly GUIContent PlatformSelection =
                new GUIContent("Platform", "Select the target platform.");

#if UNITY_2019_1_OR_NEWER
            public static readonly GUIContent SaveButton = EditorGUIUtility.TrIconContent("SaveAs", "Save current report to json file");
            public static readonly GUIContent LoadButton = EditorGUIUtility.TrIconContent("Import", "Load report from json file");
            public static readonly GUIContent DiscardButton = EditorGUIUtility.TrIconContent("TreeEditor.Trash", "Discard the current report.");
#else
            public static readonly GUIContent SaveButton = new GUIContent("Save", "Save current report to json file");
            public static readonly GUIContent LoadButton = new GUIContent("Load", "Load report from json file");
            public static readonly GUIContent DiscardButton = new GUIContent("Discard", "Discard the current report.");
#endif

            public static readonly GUIContent HelpButton = EditorGUIUtility.TrIconContent("_Help", "Open Manual (in a web browser)");

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

            public static readonly GUIContent WelcomeText = new GUIContent(
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
            public static readonly GUIContent ShaderCompilerMessages = new GUIContent("Messages", "Show Shader Compiler Messages");
            public static readonly GUIContent ShaderVariants = new GUIContent("Variants", "Inspect Shader Variants");

            public static readonly GUIContent BuildFiles = new GUIContent("Build Size");
            public static readonly GUIContent BuildSteps = new GUIContent("Build Steps");
        }

        [PostProcessBuild(1)]
        public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
        {
            if (Application.isBatchMode)
                return;

#if UNITY_2019_3_OR_NEWER
            // do nothing if ProjectAuditorWindow is not already opened
            if (!HasOpenInstances<ProjectAuditorWindow>())
                return;
#endif

            if (Instance != null)
                Instance.OnPostprocessBuild(target);
        }
    }
}
