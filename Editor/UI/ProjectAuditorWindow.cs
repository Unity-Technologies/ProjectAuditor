using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.ProjectAuditor.Editor.UI.Framework;
using Unity.ProjectAuditor.Editor.Modules;
using Unity.ProjectAuditor.Editor.AssemblyUtils;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Serialization;

namespace Unity.ProjectAuditor.Editor.UI
{
    class ProjectAuditorWindow : EditorWindow, IHasCustomMenu, IProjectIssueFilter
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

            Everything = ~0
        }

        static readonly string[] AreaNames = Enum.GetNames(typeof(Area));
        static ProjectAuditorWindow m_Instance;

        public static ProjectAuditorWindow Instance
        {
            get
            {
                if (m_Instance == null)
                    m_Instance = ShowWindow();
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
        [SerializeField] ViewStates m_ViewStates = new ViewStates();
        [SerializeField] ViewManager m_ViewManager;

        AnalysisView activeView => m_ViewManager.GetActiveView();

        public bool Match(ProjectIssue issue)
        {
            // return false if the issue does not match one of these criteria:
            // - assembly name, if applicable
            // - area
            // - is not muted, if enabled
            // - critical context, if enabled/applicable

            var viewDesc = activeView.desc;

            Profiler.BeginSample("MatchAssembly");
            var matchAssembly = !viewDesc.showAssemblySelection ||
                m_AssemblySelection != null &&
                (m_AssemblySelection.Contains(viewDesc.getAssemblyName(issue)) ||
                    m_AssemblySelection.ContainsGroup("All"));
            Profiler.EndSample();
            if (!matchAssembly)
                return false;

            Profiler.BeginSample("MatchArea");
            var matchArea = !viewDesc.showAreaSelection ||
                m_AreaSelection.ContainsAny(issue.descriptor.areas) ||
                m_AreaSelection.ContainsGroup("All");
            Profiler.EndSample();
            if (!matchArea)
                return false;

            if (!m_ViewStates.mutedIssues && viewDesc.showMuteOptions)
            {
                Profiler.BeginSample("IsMuted");
                var muted = m_ProjectAuditor.config.GetAction(issue.descriptor, issue.GetContext()) ==
                    Severity.None;
                Profiler.EndSample();
                if (muted)
                    return false;
            }

            if (viewDesc.showCritical &&
                m_ViewStates.onlyCriticalIssues &&
                !issue.isCritical)
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
            if (currentState == AnalysisState.Valid &&
                m_ProjectReport.GetAllIssues().All(i => i.IsValid()))
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
            RefreshWindow();
            Profiler.EndSample();
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
                var viewDesc = m_ViewManager.GetView(i).desc;
                ProjectAuditorAnalytics.SendEvent(
                    (ProjectAuditorAnalytics.UIButton)viewDesc.analyticsEvent,
                    ProjectAuditorAnalytics.BeginAnalytic());
                if (!m_ProjectReport.HasCategory(activeView.desc.category) && EditorUtility.DisplayDialog("Project Auditor", $"Would you like to analyze {ProjectAuditor.GetCategoryName(viewDesc.category)} now?", "Ok", "No"))
                {
                    AuditCategories(new[] {viewDesc.category});
                }
            };

            m_ViewManager.onAnalyze += category =>
            {
                AuditCategories(new[] {category});
            };
            m_ViewManager.onViewExported += () =>
            {
                ProjectAuditorAnalytics.SendEvent(ProjectAuditorAnalytics.UIButton.Export,
                    ProjectAuditorAnalytics.BeginAnalytic());
            };

            Profiler.BeginSample("Views Creation");

            var dropdownItems = new List<Utility.DropdownItem>(categories.Length);
            m_ViewManager.Create(m_ProjectAuditor, m_ViewStates, (desc, isSupported) =>
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

            using (new EditorGUILayout.VerticalScope())
            {
                if (m_AnalysisState != AnalysisState.Initializing && m_AnalysisState != AnalysisState.Initialized)
                {
                    DrawToolbar();
                }

                if (IsAnalysisValid())
                {
                    DrawPanels();

                    if (m_ViewManager.GetActiveView().desc.category != IssueCategory.MetaData)
                    {
                        GUILayout.FlexibleSpace();

                        DrawStatusBar();
                    }
                }
                else
                {
                    DrawHome();
                }
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
                category = IssueCategory.AssetDiagnostic,
                name = "Assets Diagnostics",
                menuLabel = "Assets/Diagnostics",
                menuOrder = 1,
                descriptionWithIcon = true,
                showDependencyView = true,
                showFilters = true,
                dependencyViewGuiContent = new GUIContent("Asset Dependencies"),
                onOpenIssue = EditorInterop.FocusOnAssetInProjectWindow,
                analyticsEvent = (int)ProjectAuditorAnalytics.UIButton.Assets,
                type = typeof(DiagnosticView),
            });
            ViewDescriptor.Register(new ViewDescriptor
            {
                category = IssueCategory.Shader,
                name = "Shaders",
                menuOrder = 2,
                menuLabel = "Assets/Shaders/Shaders",
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
                onOpenIssue = EditorInterop.FocusOnAssetInProjectWindow,
                onDrawToolbar = (viewManager) =>
                {
                    AnalysisView.DrawToolbarButton(Contents.ShaderCompilerMessages, () => viewManager.ChangeView(IssueCategory.ShaderCompilerMessage));
                    AnalysisView.DrawToolbarButton(Contents.ShaderVariants, () => viewManager.ChangeView(IssueCategory.ShaderVariant));
                },
                analyticsEvent = (int)ProjectAuditorAnalytics.UIButton.Shaders
            });

#if UNITY_2019_1_OR_NEWER
            ViewDescriptor.Register(new ViewDescriptor
            {
                category = IssueCategory.ShaderCompilerMessage,
                name = "Shader Messages",
                menuLabel = "Assets/Shaders/Compiler Messages",
                menuOrder = 4,
                descriptionWithIcon = true,
                onOpenIssue = EditorInterop.OpenTextFile<Shader>,
                onDrawToolbar = (viewManager) =>
                {
                    AnalysisView.DrawToolbarButton(Contents.Shaders, () => viewManager.ChangeView(IssueCategory.Shader));
                    AnalysisView.DrawToolbarButton(Contents.ShaderVariants, () => viewManager.ChangeView(IssueCategory.ShaderVariant));
                },
                analyticsEvent = (int)ProjectAuditorAnalytics.UIButton.ShaderCompilerMessages
            });

#endif

            ViewDescriptor.Register(new ViewDescriptor
            {
                category = IssueCategory.ShaderVariant,
                name = "Variants",
                menuOrder = 3,
                menuLabel = "Assets/Shaders/Variants",
                showFilters = true,
                showInfoPanel = true,
                onOpenIssue = EditorInterop.FocusOnAssetInProjectWindow,
                onDrawToolbar = (viewManager) =>
                {
                    GUILayout.FlexibleSpace();

                    AnalysisView.DrawToolbarButton(Contents.Refresh, () => Instance.AnalyzeShaderVariants());
                    AnalysisView.DrawToolbarButton(Contents.Clear, () => Instance.ClearShaderVariants());

                    GUILayout.FlexibleSpace();

                    AnalysisView.DrawToolbarButton(Contents.ShaderCompilerMessages, () => viewManager.ChangeView(IssueCategory.ShaderCompilerMessage));
                    AnalysisView.DrawToolbarButton(Contents.Shaders, () => viewManager.ChangeView(IssueCategory.Shader));
                },
                type = typeof(ShaderVariantsView),
                analyticsEvent = (int)ProjectAuditorAnalytics.UIButton.ShaderVariants
            });

            ViewDescriptor.Register(new ViewDescriptor
            {
                category = IssueCategory.ComputeShaderVariant,
                name = "Compute Shader Variants",
                menuOrder = 3,
                menuLabel = "Assets/Shaders/Compute Variants",
                showFilters = true,
                showInfoPanel = true,
                onOpenIssue = EditorInterop.FocusOnAssetInProjectWindow,
                onDrawToolbar = (viewManager) =>
                {
                    GUILayout.FlexibleSpace();

                    AnalysisView.DrawToolbarButton(Contents.Refresh, () => Instance.AnalyzeShaderVariants());
                    AnalysisView.DrawToolbarButton(Contents.Clear, () => Instance.ClearShaderVariants());

                    GUILayout.FlexibleSpace();
                },
                type = typeof(ShaderVariantsView),
                analyticsEvent = (int)ProjectAuditorAnalytics.UIButton.ComputeShaderVariants
            });

            ViewDescriptor.Register(new ViewDescriptor
            {
                category = IssueCategory.Package,
                name = "Installed Packages",
                menuLabel = "Experimental/Packages/Installed",
                menuOrder = 105,
                onDrawToolbar = (viewManager) =>
                {
                    AnalysisView.DrawToolbarButton(Contents.PackageDiagnostics, () => viewManager.ChangeView(IssueCategory.PackageDiagnostic));
                },
                onOpenIssue = EditorInterop.OpenPackage,
                showDependencyView = true,
                dependencyViewGuiContent = new GUIContent("Package Dependencies"),
                analyticsEvent = (int)ProjectAuditorAnalytics.UIButton.Packages
            });

            ViewDescriptor.Register(new ViewDescriptor
            {
                category = IssueCategory.PackageDiagnostic,
                name = "Package Diagnostics",
                menuLabel = "Experimental/Packages/Diagnostics",
                menuOrder = 106,
                onDrawToolbar = (viewManager) =>
                {
                    AnalysisView.DrawToolbarButton(Contents.Packages, () => viewManager.ChangeView(IssueCategory.Package));
                },
                onOpenIssue = EditorInterop.OpenPackage,
                type = typeof(DiagnosticView),
                analyticsEvent = (int)ProjectAuditorAnalytics.UIButton.PackageVersion
            });

            ViewDescriptor.Register(new ViewDescriptor
            {
                category = IssueCategory.Texture,
                name = "Textures",
                menuLabel = "Assets/Textures/Textures",
                menuOrder = 6,
                descriptionWithIcon = true,
                showFilters = true,
                onOpenIssue = EditorInterop.FocusOnAssetInProjectWindow,
                onDrawToolbar = (viewManager) =>
                {
                    AnalysisView.DrawToolbarButton(Contents.TextureDiagnostics, () => viewManager.ChangeView(IssueCategory.AssetDiagnostic));
                },
                analyticsEvent = (int)ProjectAuditorAnalytics.UIButton.Textures
            });

            if (UserPreferences.developerMode)
            {
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
                    onOpenIssue = EditorInterop.OpenTextFile<TextAsset>,
                    analyticsEvent = (int)ProjectAuditorAnalytics.UIButton.Generics
                });

                ViewDescriptor.Register(new ViewDescriptor
                {
                    category = IssueCategory.AudioClip,
                    name = "AudioClip",
                    menuLabel = "Experimental/Audio Clips",
                    menuOrder = 107,
                    descriptionWithIcon = true,
                    showFilters = true,
                    onOpenIssue = EditorInterop.FocusOnAssetInProjectWindow,
                    analyticsEvent = (int)ProjectAuditorAnalytics.UIButton.AudioClip
                });

                ViewDescriptor.Register(new ViewDescriptor
                {
                    category = IssueCategory.PrecompiledAssembly,
                    name = "Precompiled Assemblies",
                    menuLabel = "Experimental/Precompiled Assemblies",
                    menuOrder = 91,
                    showFilters = true,
                    getAssemblyName = issue => issue.description,
                    onOpenIssue = EditorInterop.FocusOnAssetInProjectWindow,
                    analyticsEvent = (int)ProjectAuditorAnalytics.UIButton.PrecompiledAssemblies
                });

                ViewDescriptor.Register(new ViewDescriptor
                {
                    category = IssueCategory.Mesh,
                    name = "Meshes",
                    menuLabel = "Assets/Meshes/Meshes",
                    menuOrder = 7,
                    descriptionWithIcon = true,
                    showFilters = true,
                    onOpenIssue = EditorInterop.FocusOnAssetInProjectWindow,
                    onDrawToolbar = (viewManager) =>
                    {
                        AnalysisView.DrawToolbarButton(Contents.MeshDiagnostics, () => viewManager.ChangeView(IssueCategory.AssetDiagnostic));
                    },
                    analyticsEvent = (int)ProjectAuditorAnalytics.UIButton.Meshes
                });
            }
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
                onOpenIssue = EditorInterop.FocusOnAssetInProjectWindow,
                analyticsEvent = (int)ProjectAuditorAnalytics.UIButton.Assemblies
            });
            ViewDescriptor.Register(new ViewDescriptor
            {
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
                dependencyViewGuiContent = new GUIContent("Inverted Call Hierarchy"),
                getAssemblyName = issue => issue.GetCustomProperty(CodeProperty.Assembly),
                onDrawToolbar = (viewManager) =>
                {
                    AnalysisView.DrawToolbarButton(Contents.CodeCompilerMessages, () => viewManager.ChangeView(IssueCategory.CodeCompilerMessage));
                },
                onOpenIssue = EditorInterop.OpenTextFile<TextAsset>,
                onOpenManual = EditorInterop.OpenCodeDescriptor,
                type = typeof(CodeDiagnosticView),
                analyticsEvent = (int)ProjectAuditorAnalytics.UIButton.ApiCalls
            });
            ViewDescriptor.Register(new ViewDescriptor
            {
                category = IssueCategory.CodeCompilerMessage,
                name = "Compiler Messages",
                menuOrder = 98,
                menuLabel = "Code/C# Compiler Messages",
                //showAssemblySelection = true,
                showFilters = true,
                showInfoPanel = true,
                //getAssemblyName = issue => issue.GetCustomProperty(CompilerMessageProperty.Assembly),
                onDrawToolbar = (viewManager) =>
                {
                    AnalysisView.DrawToolbarButton(Contents.CodeDiagnostics, () => viewManager.ChangeView(IssueCategory.Code));
                },
                onOpenIssue = EditorInterop.OpenTextFile<TextAsset>,
                onOpenManual = EditorInterop.OpenCompilerMessageDescriptor,
                type = typeof(CompilerMessagesView),
                analyticsEvent = (int)ProjectAuditorAnalytics.UIButton.CodeCompilerMessages
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
                showInfoPanel = true,
                showMuteOptions = true,
                onOpenIssue = (location) =>
                {
                    var guid = AssetDatabase.AssetPathToGUID(location.Path);
                    if (string.IsNullOrEmpty(guid))
                    {
                        if (location.Path.Equals("Project/Build"))
                            BuildPlayerWindow.ShowBuildPlayerWindow();
                        else
                            EditorInterop.OpenProjectSettings(location);
                    }
                    else
                    {
                        EditorInterop.FocusOnAssetInProjectWindow(location);
                    }
                },
                type = typeof(DiagnosticView),
                analyticsEvent = (int)ProjectAuditorAnalytics.UIButton.ProjectSettings
            });
            ViewDescriptor.Register(new ViewDescriptor
            {
                category = IssueCategory.BuildStep,
                name = "Build Steps",
                menuLabel = "Build Report/Steps",
                menuOrder = 100,
                showFilters = true,
                showInfoPanel = true,
                onDrawToolbar = (viewManager) =>
                {
                    AnalysisView.DrawToolbarButton(Contents.BuildFiles, () => viewManager.ChangeView(IssueCategory.BuildFile));
                },
                type = typeof(BuildReportView),
                analyticsEvent = (int)ProjectAuditorAnalytics.UIButton.BuildSteps
            });
            ViewDescriptor.Register(new ViewDescriptor
            {
                category = IssueCategory.BuildFile,
                name = "Build Size",
                menuLabel = "Build Report/Size",
                menuOrder = 101,
                descriptionWithIcon = true,
                showFilters = true,
                showInfoPanel = true,
                onOpenIssue = EditorInterop.FocusOnAssetInProjectWindow,
                onDrawToolbar = (viewManager) =>
                {
                    AnalysisView.DrawToolbarButton(Contents.BuildSteps, () => viewManager.ChangeView(IssueCategory.BuildStep));
                },
                type = typeof(BuildReportView),
                analyticsEvent = (int)ProjectAuditorAnalytics.UIButton.BuildFiles
            });
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

            var projectAuditorParams = new ProjectAuditorParams
            {
                categories = m_SelectedModules == BuiltInModules.Everything ? null : selectedCategories,
                platform = m_Platform,
                onIncomingIssues = issues =>
                {
                    // add batch of issues
                    m_ViewManager.AddIssues(issues.ToList());
                },
                onCompleted = projectReport =>
                {
                    m_AnalysisState = AnalysisState.Completed;
                    m_ProjectReport = projectReport;

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

        void AuditSingleModule<T>() where T : ProjectAuditorModule
        {
            var module = m_ProjectAuditor.GetModule<T>();
            if (!module.isSupported)
                return;

            AuditCategories(module.categories);
        }

        void AuditCategories(IssueCategory[] categories)
        {
            // a module might report more categories than requested so we need to make sure we clean up the views accordingly
            var modules = categories.SelectMany(m_ProjectAuditor.GetModules).ToArray();
            var actualCategories = modules.SelectMany(m => m.categories).Distinct().ToArray();

            var views = actualCategories
                .Select(c => m_ViewManager.GetView(c))
                .Where(v => v != null)
                .ToArray();

            foreach (var view in views)
            {
                view.Clear();
            }

            var projectAuditorParams = new ProjectAuditorParams
            {
                categories = actualCategories,
                onIncomingIssues = issues =>
                {
                    foreach (var view in views)
                    {
                        view.AddIssues(issues);
                    }
                },
                existingReport = m_ProjectReport
            };

            var platform = m_ProjectReport.GetIssues(IssueCategory.MetaData).FirstOrDefault(i => i.description.Equals(MetaDataModule.k_KeyAnalysisTarget));
            if (platform != null)
                projectAuditorParams.platform = (BuildTarget)Enum.Parse(typeof(BuildTarget), platform.GetCustomProperty(MetaDataProperty.Value));
            m_ProjectAuditor.Audit(projectAuditorParams, new ProgressBar());
        }

        public void AnalyzeShaderVariants()
        {
            AuditSingleModule<ShadersModule>();
        }

        public void ClearShaderVariants()
        {
            m_ProjectReport.ClearIssues(IssueCategory.ShaderVariant);

            m_ViewManager.ClearView(IssueCategory.ShaderVariant);

            ShadersModule.ClearBuildData();
        }

        void RefreshWindow()
        {
            if (!IsAnalysisValid())
                return;

            m_ViewManager.MarkViewsAsDirty();

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
                requestedCategories.AddRange(m_ProjectAuditor.GetModule<CodeModule>().categories);
            if (m_SelectedModules.HasFlag(BuiltInModules.Settings))
                requestedCategories.AddRange(m_ProjectAuditor.GetModule<SettingsModule>().categories);
            if ((m_SelectedModules.HasFlag(BuiltInModules.Shaders)))
                requestedCategories.AddRange(m_ProjectAuditor.GetModule<ShadersModule>().categories);
            if ((m_SelectedModules.HasFlag(BuiltInModules.Resources)))
                requestedCategories.AddRange(m_ProjectAuditor.GetModule<AssetsModule>().categories);
            if ((m_SelectedModules.HasFlag(BuiltInModules.BuildReport)))
                requestedCategories.AddRange(m_ProjectAuditor.GetModule<BuildReportModule>().categories);

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
                m_ViewStates.filters = Utility.BoldFoldout(m_ViewStates.filters, Contents.FiltersFoldout);
                if (m_ViewStates.filters)
                {
                    EditorGUI.indentLevel++;

                    DrawAssemblyFilter();
                    DrawAreaFilter();

                    activeView.DrawSearch();

                    // this is specific to diagnostics
                    if (activeView.desc.showCritical || activeView.desc.showMuteOptions)
                    {
                        EditorGUI.BeginChangeCheck();

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.LabelField("Show :", GUILayout.ExpandWidth(true), GUILayout.Width(80));

                            if (activeView.desc.showCritical)
                            {
                                var wasShowingCritical = m_ViewStates.onlyCriticalIssues;
                                m_ViewStates.onlyCriticalIssues = EditorGUILayout.ToggleLeft("Only Critical Issues",
                                    m_ViewStates.onlyCriticalIssues, GUILayout.Width(170));

                                if (wasShowingCritical != m_ViewStates.onlyCriticalIssues)
                                {
                                    var analytic = ProjectAuditorAnalytics.BeginAnalytic();
                                    var payload = new Dictionary<string, string>
                                    {
                                        ["selected"] = m_ViewStates.onlyCriticalIssues ? "true" : "false"
                                    };
                                    ProjectAuditorAnalytics.SendEventWithKeyValues(ProjectAuditorAnalytics.UIButton.OnlyCriticalIssues,
                                        analytic, payload);
                                }
                            }

                            if (activeView.desc.showMuteOptions)
                            {
                                var wasDisplayingMuted = m_ViewStates.mutedIssues;
                                m_ViewStates.mutedIssues = EditorGUILayout.ToggleLeft("Muted Issues",
                                    m_ViewStates.mutedIssues, GUILayout.Width(120));

                                if (wasDisplayingMuted != m_ViewStates.mutedIssues)
                                {
                                    var analytic = ProjectAuditorAnalytics.BeginAnalytic();
                                    var payload = new Dictionary<string, string>
                                    {
                                        ["selected"] = m_ViewStates.mutedIssues ? "true" : "false"
                                    };
                                    ProjectAuditorAnalytics.SendEventWithKeyValues(
                                        ProjectAuditorAnalytics.UIButton.ShowMuted,
                                        analytic, payload);
                                }
                            }
                            GUILayout.FlexibleSpace();
                        }
                        if (EditorGUI.EndChangeCheck())
                            m_ShouldRefresh = true;
                    }


                    activeView.DrawFilters();

                    EditorGUI.indentLevel--;
                }
            }
        }

        void DrawActions()
        {
            if (!activeView.desc.showActions)
                return;

            using (new EditorGUILayout.VerticalScope(GUI.skin.box, GUILayout.ExpandWidth(true)))
            {
                m_ViewStates.actions = Utility.BoldFoldout(m_ViewStates.actions, Contents.ActionsFoldout);
                if (m_ViewStates.actions)
                {
                    EditorGUI.indentLevel++;

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("Selected :", GUILayout.ExpandWidth(true), GUILayout.Width(80));

                        using (new EditorGUI.DisabledScope(!activeView.desc.showMuteOptions))
                        {
                            var selectedIssues = activeView.GetSelection();
                            if (GUILayout.Button(Contents.MuteButton, GUILayout.ExpandWidth(true), GUILayout.Width(100)))
                            {
                                var analytic = ProjectAuditorAnalytics.BeginAnalytic();
                                foreach (var issue in selectedIssues)
                                {
                                    SetRuleForItem(issue, Severity.None);
                                }

                                activeView.ClearSelection();

                                ProjectAuditorAnalytics.SendEventWithSelectionSummary(ProjectAuditorAnalytics.UIButton.Mute,
                                    analytic, selectedIssues);
                            }

                            if (GUILayout.Button(Contents.UnmuteButton, GUILayout.ExpandWidth(true), GUILayout.Width(100)))
                            {
                                var analytic = ProjectAuditorAnalytics.BeginAnalytic();
                                foreach (var issue in selectedIssues)
                                {
                                    ClearRulesForItem(issue);
                                }

                                ProjectAuditorAnalytics.SendEventWithSelectionSummary(
                                    ProjectAuditorAnalytics.UIButton.Unmute, analytic, selectedIssues);
                            }
                        }
                        GUILayout.FlexibleSpace();
                    }

                    EditorGUI.indentLevel--;
                }
            }
        }

        void DrawHome()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));

            EditorGUILayout.LabelField(Contents.WelcomeText, SharedStyles.TextAreaWithDynamicSize);
            EditorGUILayout.Space();

            m_SelectedModules = (BuiltInModules)EditorGUILayout.EnumFlagsField(Contents.ModulesSelection, m_SelectedModules, GUILayout.ExpandWidth(true));

            var selectedTarget = Array.IndexOf(m_SupportedBuildTargets, m_Platform);
            selectedTarget = EditorGUILayout.Popup(Contents.PlatformSelection, selectedTarget, m_PlatformContents);
            m_Platform = m_SupportedBuildTargets[selectedTarget];

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
                    LoadReport();
                }
            }

            EditorGUILayout.Space();

            EditorGUILayout.EndVertical();
        }

        void DrawPanels()
        {
            DrawReport();
        }

        void DrawStatusBar()
        {
            using (new EditorGUILayout.HorizontalScope(GUILayout.Height(20)))
            {
                var selectedIssues = activeView.GetSelection();
                var info = selectedIssues.Length + " / " + activeView.numFilteredIssues + " Items selected";
                EditorGUILayout.LabelField(info, GUILayout.ExpandWidth(true), GUILayout.Width(200));

                GUILayout.FlexibleSpace();

                EditorGUILayout.LabelField(Utility.GetIcon(Utility.IconType.ZoomTool), EditorStyles.label,
                    GUILayout.ExpandWidth(false), GUILayout.Width(20));

                var fontSize = (int)GUILayout.HorizontalSlider(m_ViewStates.fontSize, ViewStates.k_MinFontSize,
                    ViewStates.k_MaxFontSize, GUILayout.ExpandWidth(false),
                    GUILayout.Width(AnalysisView.toolbarButtonSize));
                if (fontSize != m_ViewStates.fontSize)
                {
                    m_ViewStates.fontSize = fontSize;
                    SharedStyles.SetFontDynamicSize(m_ViewStates.fontSize);
                }

                EditorGUILayout.LabelField("Ver. " + ProjectAuditor.PackageVersion, EditorStyles.label, GUILayout.Width(120));
            }
        }

        void DrawReport()
        {
            activeView.DrawTopPanel();

            if (activeView.IsValid())
            {
                DrawFilters();
                DrawActions();

                if (m_ShouldRefresh || m_AnalysisState == AnalysisState.Completed)
                {
                    RefreshWindow();
                    m_ShouldRefresh = false;
                }

                activeView.DrawContent(true);
            }
        }

        internal void SetAreaSelection(TreeViewSelection selection)
        {
            m_AreaSelection = selection;
            RefreshWindow();
        }

        internal void SetAssemblySelection(TreeViewSelection selection)
        {
            m_AssemblySelection = selection;
            RefreshWindow();
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

        void SetRuleForItem(ProjectIssue issue, Severity ruleSeverity)
        {
            var descriptor = issue.descriptor;
            var context = issue.GetContext();
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

        void ClearRulesForItem(ProjectIssue issue)
        {
            var descriptor = issue.descriptor;
            m_ProjectAuditor.config.ClearRules(descriptor, issue.GetContext());
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
                    GUILayout.Label(Utility.GetIcon(Utility.IconType.StatusWheel));
                }

                EditorGUILayout.Space();

                // right-end buttons
                using (new EditorGUI.DisabledScope(m_AnalysisState != AnalysisState.Valid))
                {
                    const int loadSaveButtonWidth = 60;
                    if (GUILayout.Button(Contents.LoadButton, EditorStyles.toolbarButton, GUILayout.Width(loadSaveButtonWidth)))
                    {
                        LoadReport();
                    }

                    if (GUILayout.Button(Contents.SaveButton, EditorStyles.toolbarButton, GUILayout.Width(loadSaveButtonWidth)))
                    {
                        SaveReport();
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

        void SaveReport()
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

        void LoadReport()
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

        public void AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(Contents.PreferencesMenuItem, false, OpenPreferences);
        }

        static void OpenPreferences()
        {
            var preferencesWindow = SettingsService.OpenUserPreferences(UserPreferences.Path);
            if (preferencesWindow == null)
            {
                Debug.LogError("Could not find Preferences for 'Analysis/Project Auditor'");
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
            public static readonly int MinWindowHeight = 540;
            public static readonly int FilterOptionsLeftLabelWidth = 100;
            public static readonly int FilterOptionsEnumWidth = 50;
        }

        static class Contents
        {
            public static readonly GUIContent WindowTitle = new GUIContent("Project Auditor");

            public static readonly GUIContent AnalyzeButton =
                new GUIContent("Analyze", "Analyze Project and list all issues found.");
            public static readonly GUIContent ModulesSelection =
                new GUIContent("Modules", "Select Project Auditor modules.");
            public static readonly GUIContent PlatformSelection =
                new GUIContent("Platform", "Select the target platform.");

#if UNITY_2019_1_OR_NEWER
            public static readonly GUIContent SaveButton = Utility.GetIcon(Utility.IconType.Save, "Save current report to json file");
            public static readonly GUIContent LoadButton = Utility.GetIcon(Utility.IconType.Load, "Load report from json file");
            public static readonly GUIContent DiscardButton = Utility.GetIcon(Utility.IconType.Trash, "Discard the current report.");
#else
            public static readonly GUIContent SaveButton = new GUIContent("Save", "Save current report to json file");
            public static readonly GUIContent LoadButton = new GUIContent("Load", "Load report from json file");
            public static readonly GUIContent DiscardButton = new GUIContent("Discard", "Discard the current report.");
#endif

            public static readonly GUIContent HelpButton = Utility.GetIcon(Utility.IconType.Help, "Open Manual (in a web browser)");
            public static readonly GUIContent PreferencesMenuItem = EditorGUIUtility.TrTextContent("Preferences", "Open User Preferences for Project Auditor");

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

            public static readonly GUIContent Clear = new GUIContent("Clear");
            public static readonly GUIContent Refresh = new GUIContent("Refresh");

            public static readonly GUIContent CodeDiagnostics = new GUIContent("Diagnostics", "Code Diagnostics");
            public static readonly GUIContent CodeCompilerMessages = new GUIContent("Messages", "Compiler Messages");

            public static readonly GUIContent Shaders = new GUIContent("Shaders", "Inspect Shaders");
            public static readonly GUIContent ShaderCompilerMessages = new GUIContent("Messages", "Show Shader Compiler Messages");
            public static readonly GUIContent ShaderVariants = new GUIContent("Variants", "Inspect Shader Variants");

            public static readonly GUIContent Textures = new GUIContent("Textures");
            public static readonly GUIContent TextureDiagnostics = new GUIContent("Diagnostics", "Texture Diagnostics");

            public static readonly GUIContent Meshes = new GUIContent("Meshes");
            public static readonly GUIContent MeshDiagnostics = new GUIContent("Diagnostics", "Mesh Diagnostics");

            public static readonly GUIContent BuildFiles = new GUIContent("Build Size");
            public static readonly GUIContent BuildSteps = new GUIContent("Build Steps");

            public static readonly GUIContent Packages = new GUIContent("Packages", "Installed Packages");
            public static readonly GUIContent PackageDiagnostics = new GUIContent("Diagnostics", "Package Diagnostics");
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
