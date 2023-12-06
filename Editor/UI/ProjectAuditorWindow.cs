using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.ProjectAuditor.Editor.UI.Framework;
using Unity.ProjectAuditor.Editor.Modules;
using Unity.ProjectAuditor.Editor.AssemblyUtils;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using Unity.ProjectAuditor.Editor.Interfaces;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

namespace Unity.ProjectAuditor.Editor.UI
{
    class ProjectAuditorWindow : EditorWindow, IHasCustomMenu, IIssueFilter
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
        enum ProjectAreaFlags
        {
            None = 0,
            Code = 1 << 0,
            Settings = 1 << 1,
            Assets = 1 << 2,
            Shaders = 1 << 3,
            Build = 1 << 4,

            // this is just helper enum to display All instead of Every
            All = ~None
        }

        const ProjectAreaFlags k_ProjectAreaDefaultFlags = ProjectAreaFlags.Code | ProjectAreaFlags.Settings | ProjectAreaFlags.Assets | ProjectAreaFlags.Shaders | ProjectAreaFlags.Build;

        static readonly string[] AreaNames = Enum.GetNames(typeof(Areas)).Where(a => a != "None" && a != "All").ToArray();
        static ProjectAuditorWindow s_Instance;

        public static ProjectAuditorWindow Instance
        {
            get
            {
                if (s_Instance == null)
                    s_Instance = ShowWindow();
                return s_Instance;
            }
        }

        GUIContent[] m_PlatformContents;
        BuildTarget[] m_SupportedBuildTargets;
        ProjectAuditor m_ProjectAuditor;
        bool m_ShouldRefresh;
        AnalyticsReporter.Analytic m_AnalyzeButtonAnalytic;
        AnalyticsReporter.Analytic m_LoadButtonAnalytic;

        // UI
        TreeViewSelection m_AreaSelection;
        TreeViewSelection m_AssemblySelection;
        Draw2D m_Draw2D;

        Areas m_SelectedAreas;

        // Serialized fields
        [SerializeField] BuildTarget m_Platform = BuildTarget.NoTarget;
        [SerializeField] CompilationMode m_CompilationMode = CompilationMode.Player;
        [SerializeField] ProjectAreaFlags m_SelectedProjectAreas = k_ProjectAreaDefaultFlags;
        [SerializeField] string m_AreaSelectionSummary;
        [SerializeField] string[] m_AssemblyNames;
        [SerializeField] string m_AssemblySelectionSummary;
        [SerializeField] ProjectReport m_ProjectReport;
        [SerializeField] AnalysisState m_AnalysisState = AnalysisState.Initializing;
        [SerializeField] ViewStates m_ViewStates = new ViewStates();
        [SerializeField] ViewManager m_ViewManager;

        enum TabId
        {
            Summary,
            Code,
            Assets,
            Shaders,
            Settings,
            Build,
        }

        [Serializable]
        class Tab
        {
            public TabId id;
            public string name;

            public IssueCategory[] categories;
            public Type[] modules;
            public IssueCategory[] excludedModuleCategories;

            public IssueCategory[] allCategories;
            public IssueCategory[] availableCategories;
            public int currentCategoryIndex;
            public Utility.DropdownItem[] dropdown;
        }

        [SerializeField]
        Tab[] m_Tabs =
        {
            new Tab
            {
                id = TabId.Summary, name = "Summary", allCategories = new[] { IssueCategory.Metadata }
            },
            new Tab
            {
                id = TabId.Code, name = "Code",
                modules = new[]
                {
                    typeof(CodeModule)
                }
            },
            new Tab
            {
                id = TabId.Assets, name = "Assets",
                categories = new[]
                {
                    IssueCategory.AssetDiagnostic, IssueCategory.Texture, IssueCategory.Mesh, IssueCategory.AudioClip, IssueCategory.AnimatorController, IssueCategory.AnimationClip, IssueCategory.Avatar, IssueCategory.AvatarMask
                }
            },
            new Tab
            {
                id = TabId.Shaders, name = "Shaders",
                modules = new[]
                {
                    typeof(ShadersModule)
                },
                excludedModuleCategories = new[]
                {
                    IssueCategory.AssetDiagnostic
                }
            },
            new Tab
            {
                id = TabId.Settings, name = "Project",
                modules = new[]
                {
                    typeof(SettingsModule)
                }
            },
            new Tab
            {
                id = TabId.Build, name = "Build",
                modules = new[]
                {
                    typeof(BuildReportModule)
                },
                excludedModuleCategories = new[]
                {
                    IssueCategory.BuildSummary
                }
            },
        };

        AnalysisView activeView => m_ViewManager.GetActiveView();

        [SerializeField] int m_ActiveTabIndex = 0;
        int m_TabButtonControlID;

        public bool Match(ProjectIssue issue)
        {
            // return false if the issue does not match one of these criteria:
            // - assembly name, if applicable
            // - area
            // - is not muted, if enabled
            // - critical context, if enabled/applicable

            var viewDesc = activeView.Desc;

            Profiler.BeginSample("MatchAssembly");
            var matchAssembly = !viewDesc.showAssemblySelection ||
                m_AssemblySelection != null &&
                (m_AssemblySelection.Contains(viewDesc.getAssemblyName(issue)) ||
                    m_AssemblySelection.ContainsGroup("All"));
            Profiler.EndSample();
            if (!matchAssembly)
                return false;

            var isDiagnostic = issue.IsDiagnostic();
            if (!isDiagnostic)
                return true;

            // TODO: the rest of this logic is common to all diagnostic views. It should be moved to the AnalysisView

            Profiler.BeginSample("MatchArea");
            var matchArea = issue.Id.IsValid() && issue.Id.GetDescriptor().MatchesAnyAreas(m_SelectedAreas);

            Profiler.EndSample();
            if (!matchArea)
                return false;

            if (m_ViewStates.onlyCriticalIssues &&
                !issue.IsMajorOrCritical())
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

            // if platform is not selected or supported, fallback to active build target
            if (m_Platform == BuildTarget.NoTarget || !BuildPipeline.IsBuildTargetSupported(BuildPipeline.GetBuildTargetGroup(m_Platform), m_Platform))
                m_Platform = EditorUserBuildSettings.activeBuildTarget;

            AnalyticsReporter.EnableAnalytics();

            Profiler.BeginSample("Update Selections");
            UpdateAreaSelection();
            UpdateAssemblySelection();
            Profiler.EndSample();

            // are we reloading from a valid state?
            if (currentState == AnalysisState.Valid &&
                m_ProjectReport.IsValid())
            {
                m_ProjectAuditor = new ProjectAuditor();

                InitializeViews(GetAllSupportedCategories(), ProjectAuditorSettings.instance.Rules, true);

                Profiler.BeginSample("Views Update");
                m_ViewManager.OnAnalysisRestored(m_ProjectReport);
                m_AnalysisState = currentState;
                Profiler.EndSample();
            }
            else
            {
                m_ActiveTabIndex = 0;
                m_AnalysisState = AnalysisState.Initialized;
            }

            m_Draw2D = new Draw2D("Unlit/ProjectAuditor");

            Profiler.BeginSample("Refresh");
            RefreshWindow();
            Profiler.EndSample();

            wantsMouseMove = true;
        }

        void InitializeViews(IssueCategory[] categories, SeverityRules rules, bool reload)
        {
            var initialize = m_ViewManager == null || !reload;

            if (initialize)
            {
                var viewDescriptors = ViewDescriptor.GetAll()
                    .Where(descriptor => categories.Contains(descriptor.category)).ToArray();
                Array.Sort(viewDescriptors, (a, b) => a.menuOrder.CompareTo(b.menuOrder));

                m_ViewManager = new ViewManager(viewDescriptors.Select(d => d.category).ToArray()); // view manager needs sorted categories
            }

            m_ViewManager.OnActiveViewChanged += i =>
            {
                var viewDesc = m_ViewManager.GetView(i).Desc;
                AnalyticsReporter.SendEvent(
                    (AnalyticsReporter.UIButton)viewDesc.analyticsEvent,
                    AnalyticsReporter.BeginAnalytic());
            };

            m_ViewManager.OnActiveViewChanged += i =>
            {
                var viewDesc = m_ViewManager.GetView(i).Desc;
                SyncTabOnViewChange(viewDesc.category);
            };

            m_ViewManager.OnIgnoredIssuesVisibilityChanged += showIgnoredIssues =>
            {
                var analytic = AnalyticsReporter.BeginAnalytic();
                var payload = new Dictionary<string, string>
                {
                    ["selected"] = showIgnoredIssues ? "true" : "false"
                };
                AnalyticsReporter.SendEventWithKeyValues(
                    AnalyticsReporter.UIButton.ShowMuted,
                    analytic, payload);
            };

            m_ViewManager.OnSelectedIssuesIgnoreRequested = issues =>
            {
                var analytic = AnalyticsReporter.BeginAnalytic();

                AnalyticsReporter.SendEventWithSelectionSummary(AnalyticsReporter.UIButton.Mute,
                    analytic, issues);
            };

            m_ViewManager.OnSelectedIssuesDisplayRequested = issues =>
            {
                var analytic = AnalyticsReporter.BeginAnalytic();

                AnalyticsReporter.SendEventWithSelectionSummary(
                    AnalyticsReporter.UIButton.Unmute, analytic, issues);
            };

            m_ViewManager.OnAnalysisRequested += category =>
            {
                AuditCategories(new[] {category});
            };

            m_ViewManager.OnViewExportCompleted += () =>
            {
                AnalyticsReporter.SendEvent(AnalyticsReporter.UIButton.Export,
                    AnalyticsReporter.BeginAnalytic());
            };

            Profiler.BeginSample("Views Creation");

            m_ViewManager.Create(rules, m_ViewStates, null, this);

            InitializeTabs(!initialize);

            Profiler.EndSample();
        }

        void InitializeTabs(bool reload)
        {
            if (!reload)
                m_ActiveTabIndex = 0;

            foreach (var tab in m_Tabs)
            {
                RefreshTabCategories(tab, reload);
            }
        }

        private void RefreshTabCategories(Tab tab, bool reload)
        {
            List<IssueCategory> availableCategories = new List<IssueCategory>();
            var dropDownItems = new List<Utility.DropdownItem>();
            var categoryIndex = 0;

            var categories = GetTabCategories(tab);

            foreach (var cat in categories)
            {
                var view = m_ViewManager.GetView(cat);
                if (view == null)
                    continue;

                var displayName = view.IsDiagnostic() ? "Diagnostics" : view.Desc.displayName;

                dropDownItems.Add(new Utility.DropdownItem
                {
                    Content = new GUIContent(displayName),
                    SelectionContent = new GUIContent("View: " + displayName),
                    Enabled = true,
                    UserData = categoryIndex++
                });

                availableCategories.Add(cat);
            }

            if (dropDownItems.Count > 1)
                tab.dropdown = dropDownItems.ToArray();
            else
                tab.dropdown = null;

            tab.availableCategories = availableCategories.ToArray();

            if (!reload)
                tab.currentCategoryIndex = 0;
        }

        void SyncTabOnViewChange(IssueCategory newCatagory)
        {
            for (int tabIndex = 0; tabIndex < m_Tabs.Length; ++tabIndex)
            {
                for (int categoryIndex = 0; categoryIndex < m_Tabs[tabIndex].availableCategories.Length; ++categoryIndex)
                {
                    if (m_Tabs[tabIndex].availableCategories[categoryIndex] == newCatagory)
                    {
                        m_ActiveTabIndex = tabIndex;
                        m_Tabs[m_ActiveTabIndex].currentCategoryIndex = categoryIndex;
                        return;
                    }
                }
            }
        }

        void OnDisable()
        {
            // Make sure 'dirty' scriptable objects are saved to their corresponding assets
            AssetDatabase.SaveAssets();

            m_ViewManager?.SaveSettings();
        }

        void OnGUI()
        {
            using (new EditorGUILayout.VerticalScope())
            {
                if (m_AnalysisState != AnalysisState.Initializing && m_AnalysisState != AnalysisState.Initialized)
                {
                    DrawToolbar();

                    DrawTabs();
                }

                if (IsAnalysisValid())
                {
                    DrawPanels();

                    if (m_ViewManager.GetActiveView().Desc.category != IssueCategory.Metadata)
                    {
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
                category = IssueCategory.Metadata,
                displayName = "Summary",
                menuOrder = -1,
                showInfoPanel = true,
                type = typeof(SummaryView),
                analyticsEvent = (int)AnalyticsReporter.UIButton.Summary
            });
            ViewDescriptor.Register(new ViewDescriptor
            {
                category = IssueCategory.AssetDiagnostic,
                displayName = "Asset Diagnostics",
                menuLabel = "Assets/Diagnostics",
                menuOrder = 1,
                descriptionWithIcon = true,
                showDependencyView = true,
                showFilters = true,
                dependencyViewGuiContent = new GUIContent("Asset Dependencies"),
                onOpenIssue = EditorInterop.FocusOnAssetInProjectWindow,
                analyticsEvent = (int)AnalyticsReporter.UIButton.Assets,
                type = typeof(DiagnosticView),
            });
            ViewDescriptor.Register(new ViewDescriptor
            {
                category = IssueCategory.Shader,
                displayName = "Shaders",
                menuOrder = 1,
                menuLabel = "Assets/Shaders/Shaders",
                descriptionWithIcon = true,
                showFilters = true,
                onContextMenu = (menu, viewManager, issue) =>
                {
                    menu.AddItem(Contents.ShaderVariants, false, () =>
                    {
                        viewManager.ChangeView(IssueCategory.ShaderVariant);
                        viewManager.GetActiveView().SetSearch(issue.Description);
                    });
                },
                onOpenIssue = EditorInterop.FocusOnAssetInProjectWindow,
                analyticsEvent = (int)AnalyticsReporter.UIButton.Shaders
            });

            ViewDescriptor.Register(new ViewDescriptor
            {
                category = IssueCategory.Material,
                displayName = "Materials",
                menuLabel = "Assets/Shaders/Materials",
                menuOrder = 2,
                showFilters = true,
                onOpenIssue = EditorInterop.FocusOnAssetInProjectWindow,
                analyticsEvent = (int)AnalyticsReporter.UIButton.Materials
            });

            ViewDescriptor.Register(new ViewDescriptor
            {
                category = IssueCategory.ShaderCompilerMessage,
                displayName = "Compiler Messages",
                menuLabel = "Assets/Shaders/Compiler Messages",
                menuOrder = 4,
                descriptionWithIcon = true,
                onOpenIssue = EditorInterop.OpenTextFile<Shader>,
                analyticsEvent = (int)AnalyticsReporter.UIButton.ShaderCompilerMessages
            });

            ViewDescriptor.Register(new ViewDescriptor
            {
                category = IssueCategory.ShaderVariant,
                displayName = "Shader Variants",
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
                },
                type = typeof(ShaderVariantsView),
                analyticsEvent = (int)AnalyticsReporter.UIButton.ShaderVariants
            });

            ViewDescriptor.Register(new ViewDescriptor
            {
                category = IssueCategory.ComputeShaderVariant,
                displayName = "Compute Shader Variants",
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
                analyticsEvent = (int)AnalyticsReporter.UIButton.ComputeShaderVariants
            });

            ViewDescriptor.Register(new ViewDescriptor
            {
                category = IssueCategory.Package,
                displayName = "Installed Packages",
                menuLabel = "Project/Packages/Installed",
                menuOrder = 105,
                onOpenIssue = EditorInterop.OpenPackage,
                showDependencyView = true,
                dependencyViewGuiContent = new GUIContent("Package Dependencies"),
                analyticsEvent = (int)AnalyticsReporter.UIButton.Packages
            });

            ViewDescriptor.Register(new ViewDescriptor
            {
                category = IssueCategory.PackageDiagnostic,
                displayName = "Package Diagnostics",
                menuLabel = "Project/Packages/Diagnostics",
                menuOrder = 106,
                onOpenIssue = EditorInterop.OpenPackage,
                type = typeof(DiagnosticView),
                analyticsEvent = (int)AnalyticsReporter.UIButton.PackageDiagnostics
            });

            ViewDescriptor.Register(new ViewDescriptor
            {
                category = IssueCategory.AudioClip,
                displayName = "AudioClips",
                menuLabel = "Assets/Audio Clips",
                menuOrder = 107,
                descriptionWithIcon = true,
                showFilters = true,
                onOpenIssue = EditorInterop.FocusOnAssetInProjectWindow,
                analyticsEvent = (int)AnalyticsReporter.UIButton.AudioClip
            });

            ViewDescriptor.Register(new ViewDescriptor
            {
                category = IssueCategory.Mesh,
                displayName = "Meshes",
                menuLabel = "Assets/Meshes/Meshes",
                menuOrder = 7,
                descriptionWithIcon = true,
                showFilters = true,
                onOpenIssue = EditorInterop.FocusOnAssetInProjectWindow,
                analyticsEvent = (int)AnalyticsReporter.UIButton.Meshes
            });

            ViewDescriptor.Register(new ViewDescriptor
            {
                category = IssueCategory.Texture,
                displayName = "Textures",
                menuLabel = "Assets/Textures/Textures",
                menuOrder = 6,
                descriptionWithIcon = true,
                showFilters = true,
                onOpenIssue = EditorInterop.FocusOnAssetInProjectWindow,
                analyticsEvent = (int)AnalyticsReporter.UIButton.Textures
            });

            ViewDescriptor.Register(new ViewDescriptor
            {
                category = IssueCategory.AnimatorController,
                displayName = "Animator Controllers",
                menuLabel = "Assets/Animation/Animator Controllers",
                menuOrder = 8,
                descriptionWithIcon = true,
                showFilters = true,
                onOpenIssue = EditorInterop.FocusOnAssetInProjectWindow,
                analyticsEvent = (int)AnalyticsReporter.UIButton.AnimatorControllers
            });

            ViewDescriptor.Register(new ViewDescriptor
            {
                category = IssueCategory.AnimationClip,
                displayName = "Animation Clips",
                menuLabel = "Assets/Animation/Animation Clips",
                menuOrder = 9,
                descriptionWithIcon = true,
                showFilters = true,
                onOpenIssue = EditorInterop.FocusOnAssetInProjectWindow,
                analyticsEvent = (int)AnalyticsReporter.UIButton.AnimationClips
            });

            ViewDescriptor.Register(new ViewDescriptor
            {
                category = IssueCategory.Avatar,
                displayName = "Avatars",
                menuLabel = "Assets/Animation/Avatars",
                menuOrder = 10,
                descriptionWithIcon = true,
                showFilters = true,
                onOpenIssue = EditorInterop.FocusOnAssetInProjectWindow,
                analyticsEvent = (int)AnalyticsReporter.UIButton.Avatars
            });

            ViewDescriptor.Register(new ViewDescriptor
            {
                category = IssueCategory.AvatarMask,
                displayName = "Avatar Masks",
                menuLabel = "Assets/Animation/Avatar Masks",
                menuOrder = 11,
                descriptionWithIcon = true,
                showFilters = true,
                onOpenIssue = EditorInterop.FocusOnAssetInProjectWindow,
                analyticsEvent = (int)AnalyticsReporter.UIButton.AvatarMasks
            });

            if (UserPreferences.DeveloperMode)
            {
                ViewDescriptor.Register(new ViewDescriptor
                {
                    category = IssueCategory.GenericInstance,
                    displayName = "Generics",
                    menuLabel = "Experimental/Generic Types Instantiation",
                    menuOrder = 90,
                    showAssemblySelection = true,
                    showDependencyView = true,
                    showFilters = true,
                    dependencyViewGuiContent = new GUIContent("Inverted Call Hierarchy"),
                    getAssemblyName = issue => issue.GetCustomProperty(CodeProperty.Assembly),
                    onOpenIssue = EditorInterop.OpenTextFile<TextAsset>,
                    analyticsEvent = (int)AnalyticsReporter.UIButton.Generics
                });

                ViewDescriptor.Register(new ViewDescriptor
                {
                    category = IssueCategory.PrecompiledAssembly,
                    displayName = "Precompiled Assemblies",
                    menuLabel = "Experimental/Precompiled Assemblies",
                    menuOrder = 91,
                    showFilters = true,
                    onOpenIssue = EditorInterop.FocusOnAssetInProjectWindow,
                    analyticsEvent = (int)AnalyticsReporter.UIButton.PrecompiledAssemblies
                });
            }
            ViewDescriptor.Register(new ViewDescriptor
            {
                category = IssueCategory.Assembly,
                displayName = "Assemblies",
                menuLabel = "Code/Assemblies",
                menuOrder = 98,
                showFilters = true,
                showDependencyView = true,
                onOpenIssue = EditorInterop.FocusOnAssetInProjectWindow,
                analyticsEvent = (int)AnalyticsReporter.UIButton.Assemblies
            });
            ViewDescriptor.Register(new ViewDescriptor
            {
                category = IssueCategory.Code,
                displayName = "Code",
                menuLabel = "Code/Diagnostics",
                menuOrder = 0,
                showAssemblySelection = true,
                showDependencyView = true,
                showFilters = true,
                showInfoPanel = true,
                dependencyViewGuiContent = new GUIContent("Inverted Call Hierarchy"),
                getAssemblyName = issue => issue.GetCustomProperty(CodeProperty.Assembly),
                onOpenIssue = EditorInterop.OpenTextFile<TextAsset>,
                onOpenManual = EditorInterop.OpenCodeDescriptor,
                type = typeof(CodeDiagnosticView),
                analyticsEvent = (int)AnalyticsReporter.UIButton.ApiCalls
            });
            ViewDescriptor.Register(new ViewDescriptor
            {
                category = IssueCategory.CodeCompilerMessage,
                displayName = "Compiler Messages",
                menuOrder = 98,
                menuLabel = "Code/C# Compiler Messages",
                showFilters = true,
                showInfoPanel = true,
                onOpenIssue = EditorInterop.OpenTextFile<TextAsset>,
                onOpenManual = EditorInterop.OpenCompilerMessageDescriptor,
                type = typeof(CompilerMessagesView),
                analyticsEvent = (int)AnalyticsReporter.UIButton.CodeCompilerMessages
            });
            ViewDescriptor.Register(new ViewDescriptor
            {
                category = IssueCategory.ProjectSetting,
                displayName = "Settings",
                menuLabel = "Project/Settings/Diagnostics",
                menuOrder = 1,
                showFilters = true,
                showInfoPanel = true,
                onOpenIssue = (location) =>
                {
                    var guid = AssetDatabase.AssetPathToGUID(location.Path);
                    if (string.IsNullOrEmpty(guid))
                    {
                        EditorInterop.OpenProjectSettings(location);
                    }
                    else
                    {
                        EditorInterop.FocusOnAssetInProjectWindow(location);
                    }
                },
                type = typeof(DiagnosticView),
                analyticsEvent = (int)AnalyticsReporter.UIButton.ProjectSettings
            });
            ViewDescriptor.Register(new ViewDescriptor
            {
                category = IssueCategory.BuildStep,
                displayName = "Build Steps",
                menuLabel = "Build Report/Steps",
                menuOrder = 100,
                showFilters = true,
                showInfoPanel = true,
                type = typeof(BuildStepsView),
                analyticsEvent = (int)AnalyticsReporter.UIButton.BuildSteps
            });
            ViewDescriptor.Register(new ViewDescriptor
            {
                category = IssueCategory.BuildFile,
                displayName = "Build Size",
                menuLabel = "Build Report/Size",
                menuOrder = 101,
                descriptionWithIcon = true,
                showFilters = true,
                showInfoPanel = true,
                onOpenIssue = EditorInterop.FocusOnAssetInProjectWindow,
                type = typeof(BuildSizeView),
                analyticsEvent = (int)AnalyticsReporter.UIButton.BuildFiles
            });
            ViewDescriptor.Register(new ViewDescriptor
            {
                category = IssueCategory.DomainReload,
                displayName = "Domain Reload",
                menuLabel = "Code/Domain Reload",
                menuOrder = 50,
                showAssemblySelection = true,
                showFilters = true,
                showInfoPanel = true,
                getAssemblyName = issue => issue.GetCustomProperty(CompilerMessageProperty.Assembly),
                onOpenIssue = EditorInterop.OpenTextFile<TextAsset>,
                onOpenManual = EditorInterop.OpenCodeDescriptor,
                type = typeof(CodeDomainReloadView),
                analyticsEvent = (int)AnalyticsReporter.UIButton.DomainReload
            });
        }

        bool IsAnalysisValid()
        {
            return m_AnalysisState != AnalysisState.Initializing && m_AnalysisState != AnalysisState.Initialized;
        }

        void Analyze()
        {
            m_AnalyzeButtonAnalytic = AnalyticsReporter.BeginAnalytic();

            m_ShouldRefresh = true;
            m_AnalysisState = AnalysisState.InProgress;
            m_ProjectReport = null;

            m_ProjectAuditor = new ProjectAuditor();

            var analysisParams = new AnalysisParams
            {
                Categories = GetSelectedCategories(),
                CompilationMode = m_CompilationMode,
                Platform = m_Platform,
                OnIncomingIssues = issues =>
                {
                    // add batch of issues
                    m_ViewManager.AddIssues(issues.ToList());
                },
                OnCompleted = report =>
                {
                    if (!report.IsValid())
                    {
                        m_AnalysisState = AnalysisState.Initialized;
                        return;
                    }
                    m_ViewManager.OnAnalysisCompleted(report);

                    m_ShouldRefresh = true;
                    m_AnalysisState = AnalysisState.Completed;

                    m_ProjectReport = report;
                }
            };

            InitializeViews(GetAllSupportedCategories(), analysisParams.Rules, false);

            m_ProjectAuditor.AuditAsync(analysisParams, new ProgressBar());
        }

        void Update()
        {
            if (m_ShouldRefresh)
                Repaint();
            if (m_AnalysisState == AnalysisState.InProgress)
                Repaint();
        }

        void AuditSingleModule<T>() where T : Module
        {
            var module = m_ProjectAuditor.GetModule<T>();
            if (!module.IsSupported)
                return;

            AuditCategories(module.Categories);
        }

        void AuditCategories(IssueCategory[] categories, bool refreshSummaryView = false)
        {
            // a module might report more categories than requested so we need to make sure we clean up the views accordingly
            var modules = categories.SelectMany(m_ProjectAuditor.GetModules).ToArray();
            var actualCategories = modules.SelectMany(m => m.Categories).Distinct().ToArray();

            var views = actualCategories
                .Select(c => m_ViewManager.GetView(c))
                .Where(v => v != null)
                .ToArray();

            foreach (var view in views)
            {
                view.Clear();
            }

            var analysisParams = new AnalysisParams
            {
                Categories = actualCategories,
                CompilationMode = m_CompilationMode,
                OnIncomingIssues = issues =>
                {
                    foreach (var view in views)
                    {
                        view.AddIssues(issues);
                    }
                },
                Platform = m_ProjectReport.SessionInfo.Platform,
                ExistingReport = m_ProjectReport,
                OnCompleted = report =>
                {
                    if (!report.IsValid())
                    {
                        m_AnalysisState = AnalysisState.Initialized;
                        return;
                    }
                    m_ViewManager.OnAnalysisCompleted(report);

                    m_ShouldRefresh = true;
                    m_AnalysisState = AnalysisState.Completed;
                }
            };

            m_ProjectAuditor.Audit(analysisParams, new ProgressBar());

            if (refreshSummaryView)
            {
                var summaryView = m_ViewManager.GetView(IssueCategory.Metadata);
                if (summaryView != null)
                {
                    summaryView.Clear();
                    summaryView.AddIssues(m_ProjectReport.GetAllIssues());
                }
            }
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
                    AnalyticsReporter.SendEvent(AnalyticsReporter.UIButton.Load, m_LoadButtonAnalytic);
                if (m_AnalyzeButtonAnalytic != null)
                    AnalyticsReporter.SendEventWithAnalyzeSummary(AnalyticsReporter.UIButton.Analyze, m_AnalyzeButtonAnalytic, m_ProjectReport);

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
            return m_SelectedAreas.ToString();
        }

        IssueCategory[] GetSelectedCategories()
        {
            var requestedCategories = new List<IssueCategory>(new[] {IssueCategory.Metadata});
            if (m_SelectedProjectAreas.HasFlag(ProjectAreaFlags.Code))
                requestedCategories.AddRange(GetTabCategories(TabId.Code));
            if (m_SelectedProjectAreas.HasFlag(ProjectAreaFlags.Settings))
                requestedCategories.AddRange(GetTabCategories(TabId.Settings));
            if (m_SelectedProjectAreas.HasFlag(ProjectAreaFlags.Assets))
                requestedCategories.AddRange(GetTabCategories(TabId.Assets));
            if (m_SelectedProjectAreas.HasFlag(ProjectAreaFlags.Shaders))
                requestedCategories.AddRange(GetTabCategories(TabId.Shaders));
            if (m_SelectedProjectAreas.HasFlag(ProjectAreaFlags.Build))
                requestedCategories.AddRange(GetTabCategories(TabId.Build));

            return requestedCategories.ToArray();
        }

        IssueCategory[] GetAllSupportedCategories()
        {
            List<IssueCategory> allTabCategories = new List<IssueCategory>();
            foreach (var tab in m_Tabs)
            {
                var categories = GetTabCategories(tab);
                allTabCategories.AddRange(categories);
            }

            return allTabCategories.Distinct().ToArray();
        }

        IssueCategory[] GetTabCategories(TabId tabId)
        {
            return GetTabCategories(m_Tabs.First(t => t.id == tabId));
        }

        IssueCategory[] GetTabCategories(Tab tab)
        {
            if (tab.allCategories != null)
                return tab.allCategories;

            if (tab.modules != null && tab.modules.Length > 0)
            {
                List<IssueCategory> categories = new List<IssueCategory>();

                foreach (var moduleType in tab.modules)
                {
                    var module = m_ProjectAuditor.GetModule(moduleType);

                    if (module == null)
                        continue;

                    var tabValue = tab;

                    var moduleCategories = module.SupportedLayouts
                        .Where(l => tabValue.excludedModuleCategories == null || tabValue.excludedModuleCategories.Contains(l.category) == false)
                        .Select(l => l.category);

                    categories.AddRange(moduleCategories);
                }

                tab.allCategories = categories.Distinct().ToArray();
            }
            else
            {
                tab.allCategories = tab.categories;
            }

            return tab.allCategories;
        }

        void DrawAssemblyFilter()
        {
            if (!activeView.Desc.showAssemblySelection)
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
                            var analytic = AnalyticsReporter.BeginAnalytic();

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
                                        var selectEvent = AnalyticsReporter.BeginAnalytic();
                                        SetAssemblySelection(selection);

                                        var payload = new Dictionary<string, string>();
                                        var selectedAsmNames = selection.selection;

                                        payload["numSelected"] = selectedAsmNames.Count.ToString();
                                        payload["numUnityAssemblies"] = selectedAsmNames.Count(assemblyName => assemblyName.Contains("Unity")).ToString();

                                        AnalyticsReporter.SendEventWithKeyValues(AnalyticsReporter.UIButton.AssemblySelectApply, selectEvent, payload);
                                    });
                            }

                            AnalyticsReporter.SendEvent(AnalyticsReporter.UIButton.AssemblySelect,
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
            if (!activeView.IsDiagnostic())
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
                            var analytic = AnalyticsReporter.BeginAnalytic();

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
                                        var selectEvent = AnalyticsReporter.BeginAnalytic();
                                        SetAreaSelection(selection);

                                        var payload = new Dictionary<string, string>();
                                        payload["areas"] = GetSelectedAreasSummary();
                                        AnalyticsReporter.SendEventWithKeyValues(AnalyticsReporter.UIButton.AreaSelectApply, selectEvent, payload);
                                    });
                            }

                            AnalyticsReporter.SendEvent(AnalyticsReporter.UIButton.AreaSelect, analytic);
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
            if (!activeView.Desc.showFilters)
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

                    activeView.DrawFilters();

                    EditorGUI.indentLevel--;
                }
            }
        }

        void DrawHome()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));

            EditorGUILayout.Space();

            EditorGUILayout.LabelField(Contents.WelcomeTextTitle, SharedStyles.TitleLabel);

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            EditorGUILayout.LabelField(Contents.WelcomeText, SharedStyles.TextAreaWithDynamicSize);

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            DrawHorizontalLine();

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            EditorGUILayout.LabelField(Contents.ConfigurationsTitle, SharedStyles.LargeLabel);

            EditorGUILayout.Space();

            m_SelectedProjectAreas = (ProjectAreaFlags)EditorGUILayout.EnumFlagsField(Contents.ProjectAreaSelection, m_SelectedProjectAreas, GUILayout.ExpandWidth(true));

            var selectedTarget = Array.IndexOf(m_SupportedBuildTargets, m_Platform);
            selectedTarget = EditorGUILayout.Popup(Contents.PlatformSelection, selectedTarget, m_PlatformContents);
            m_Platform = m_SupportedBuildTargets[selectedTarget];

            m_CompilationMode = (CompilationMode)EditorGUILayout.EnumPopup(Contents.CompilationModeSelection, m_CompilationMode);

            GUILayout.FlexibleSpace();

            using (new EditorGUILayout.HorizontalScope())
            {
                const int height = 30;

                GUILayout.FlexibleSpace();

                using (new EditorGUI.DisabledScope(m_SelectedProjectAreas == ProjectAreaFlags.None))
                {
                    if (GUILayout.Button(Contents.AnalyzeButton, GUILayout.Width(100), GUILayout.Height(height)))
                    {
                        Analyze();
                        GUIUtility.ExitGUI();
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

        void DrawHorizontalLine()
        {
            var rect = EditorGUILayout.GetControlRect(GUILayout.Height(1));

            if (m_Draw2D.DrawStart(rect))
            {
                m_Draw2D.DrawLine(0, 0, rect.width, 0, Color.black);
                m_Draw2D.DrawEnd();
            }
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
                var info = selectedIssues.Length + " / " + activeView.NumFilteredIssues + " Items selected";
                EditorGUILayout.LabelField(info, GUILayout.ExpandWidth(true), GUILayout.Width(200));

                GUILayout.FlexibleSpace();

                EditorGUILayout.LabelField(Utility.GetIcon(Utility.IconType.ZoomTool), EditorStyles.label,
                    GUILayout.ExpandWidth(false), GUILayout.Width(20));

                var fontSize = (int)GUILayout.HorizontalSlider(m_ViewStates.fontSize, ViewStates.k_MinFontSize,
                    ViewStates.k_MaxFontSize, GUILayout.ExpandWidth(false),
                    GUILayout.Width(AnalysisView.ToolbarButtonSize));
                if (fontSize != m_ViewStates.fontSize)
                {
                    m_ViewStates.fontSize = fontSize;
                    SharedStyles.SetFontDynamicSize(m_ViewStates.fontSize);
                }

                EditorGUILayout.LabelField("Ver. " + ProjectAuditor.PackageVersion, EditorStyles.label, GUILayout.Width(120));
            }
        }

        void DrawViewSelection()
        {
            // Sub categories dropdown, if there's more than one view
            if (m_ViewManager != null && m_Tabs[m_ActiveTabIndex].dropdown != null)
            {
                using (new GUILayout.HorizontalScope(SharedStyles.TabBackground))
                {
                    Utility.ToolbarDropdownList(m_Tabs[m_ActiveTabIndex].dropdown,
                        m_Tabs[m_ActiveTabIndex].currentCategoryIndex,
                        (arg) =>
                        {
                            var categoryIndex = (int)arg;
                            if (m_ProjectReport == null)
                                return; // this happens from the summary view while the report is being generated

                            var category = m_Tabs[m_ActiveTabIndex]
                                .availableCategories[categoryIndex];
                            if (category != IssueCategory.Metadata && !m_ProjectReport.HasCategory(category))
                            {
                                var displayName = m_ViewManager.GetView(category).Desc.displayName;
                                if (!EditorUtility.DisplayDialog(ProjectAuditor.DisplayName,
                                    $"'{displayName}' analysis will now begin.", "Ok",
                                    "Cancel"))
                                    return; // do not analyze and change view

                                if (category == IssueCategory.Texture ||
                                    category == IssueCategory.Mesh
                                    || category == IssueCategory.AudioClip)
                                {
                                    List<IssueCategory> categories = new List<IssueCategory>();

                                    // For asset categories, analyze all asset categories as a workaround:
                                    // That way after the prompt above (which won't appear again for other asset categories!),
                                    // we ensure all non-diagnostic asset views are populated with asset details.
                                    if (m_ViewManager.GetView(IssueCategory.Texture) != null)
                                        categories.Add(IssueCategory.Texture);
                                    if (m_ViewManager.GetView(IssueCategory.Mesh) != null)
                                        categories.Add(IssueCategory.Mesh);
                                    if (m_ViewManager.GetView(IssueCategory.AudioClip) != null)
                                        categories.Add(IssueCategory.AudioClip);

                                    AuditCategories(categories.ToArray(), true);
                                }
                                else
                                    AuditCategories(new[] { category }, true);

                                var tab = m_Tabs[m_ActiveTabIndex];
                                RefreshTabCategories(tab, false);
                            }

                            m_ViewManager.ChangeView(category);
                        }, GUILayout.Width(180));
                }
            }
        }

        void DrawReport()
        {
            GUILayout.Space(2);

            using (new GUILayout.HorizontalScope(GUI.skin.box))
            {
                GUILayout.Label(activeView.Description, GUILayout.MinWidth(360), GUILayout.ExpandWidth(true));
                DrawViewSelection();

                GUILayout.FlexibleSpace();
            }

            activeView.DrawTopPanel(false);

            if (activeView.IsValid())
            {
                DrawFilters();

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
            var selectedStrings = selection.GetSelectedStrings(AreaNames, true);

            m_SelectedAreas = Areas.None;
            foreach (var areaString in selectedStrings)
            {
                m_SelectedAreas |= (Areas)Enum.Parse(typeof(Areas), areaString);
            }

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
                        m_SelectedAreas = Areas.All;
                    }
                    else if (m_AreaSelectionSummary != "None")
                    {
                        var areas = Formatting.SplitStrings(m_AreaSelectionSummary);
                        m_AreaSelection.selection.AddRange(areas);
                        m_SelectedAreas = (Areas)Enum.Parse(typeof(Areas), m_AreaSelectionSummary);
                    }
                }
                else
                {
                    m_AreaSelection.SetAll(AreaNames);
                    m_SelectedAreas = Areas.All;
                }
            }
        }

        void UpdateAssemblyNames()
        {
            // update list of assembly names
            var assemblyNames = m_ProjectReport.FindByCategory(IssueCategory.Assembly).Select(i => i.Description).ToArray();
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

        void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label(Utility.GetPlatformIcon(BuildPipeline.GetBuildTargetGroup(m_Platform)), SharedStyles.IconLabel, GUILayout.Width(AnalysisView.ToolbarIconSize));

                if (m_AnalysisState == AnalysisState.InProgress)
                {
                    GUILayout.Label(Utility.GetIcon(Utility.IconType.StatusWheel), SharedStyles.IconLabel, GUILayout.Width(AnalysisView.ToolbarIconSize));
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
                            m_ActiveTabIndex = 0;
                            m_AnalysisState = AnalysisState.Initialized;
                        }
                    }
                }

                Utility.DrawHelpButton(Contents.HelpButton, Documentation.GetPageUrl("index"));
            }
        }

        void DrawTabs()
        {
            int tabToAudit = -1;

            using (new EditorGUILayout.VerticalScope(SharedStyles.TabBackground))
            {
                const int tabButtonWidth = 80;
                const int tabButtonHeight = 27;

                EditorGUILayout.BeginHorizontal(SharedStyles.TabBackground);

                for (var i = 0; i < m_Tabs.Length; i++)
                {
                    GUI.enabled = m_ProjectReport != null;

                    if (DrawTabButton(new GUIContent(m_Tabs[i].name), m_ActiveTabIndex == i,
                        tabButtonWidth, tabButtonHeight))
                    {
                        var tab = m_Tabs[i];

                        var hasAnyCategories = false;
                        foreach (var category in tab.allCategories)
                        {
                            if (m_ProjectReport.HasCategory(category))
                            {
                                hasAnyCategories = true;
                            }
                        }

                        if (!hasAnyCategories)
                        {
                            tabToAudit = i;
                        }
                        else
                        {
                            if (tab.availableCategories.Length > tab.currentCategoryIndex)
                                m_ViewManager.ChangeView(tab.availableCategories[tab.currentCategoryIndex]);
                        }
                    }

                    GUI.enabled = true;
                }

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }

            if (tabToAudit != -1)
            {
                var tab = m_Tabs[tabToAudit];

                if (EditorUtility.DisplayDialog(ProjectAuditor.DisplayName,
                    $"'{tab.name}' analysis will now begin.", "Ok", "Cancel"))
                {
                    AuditCategories(tab.allCategories, true);

                    RefreshTabCategories(tab, false);

                    if (tab.availableCategories.Length > 0)
                        m_ViewManager.ChangeView(tab.availableCategories[0]);
                }
            }
        }

        bool DrawTabButton(GUIContent content, bool isActive, int width, int height)
        {
            EditorGUILayout.BeginVertical();

            bool wasButtonClicked = GUILayout.Button(content, SharedStyles.TabButton, GUILayout.Width(width), GUILayout.Height(height));
            int id = GUIUtility.GetControlID(content, FocusType.Passive);
            var isHoverState = GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition);

            if (Event.current.type == EventType.MouseMove)
            {
                if (isHoverState)
                {
                    if (m_TabButtonControlID != id)
                    {
                        m_TabButtonControlID = id;
                        if (mouseOverWindow != null)
                        {
                            mouseOverWindow.Repaint();
                        }
                    }
                }
                else
                {
                    if (m_TabButtonControlID == id)
                    {
                        m_TabButtonControlID = 0;
                        if (mouseOverWindow != null)
                        {
                            mouseOverWindow.Repaint();
                        }
                    }
                }
            }

            var rect = EditorGUILayout.GetControlRect(false, 3, SharedStyles.TabBackground, GUILayout.Width(width), GUILayout.Height(2));
            if ((isActive || isHoverState) && m_Draw2D.DrawStart(rect))
            {
                var color = isActive ? SharedStyles.TabBottomActiveColor : SharedStyles.TabBottomHoverColor;

                m_Draw2D.DrawFilledBox(0, 0, width, 2.5f, color);
                m_Draw2D.DrawEnd();
            }

            EditorGUILayout.EndVertical();

            return wasButtonClicked;
        }

        void SaveReport()
        {
            var path = EditorUtility.SaveFilePanel(k_SaveToFile, UserPreferences.LoadSavePath, "project-auditor-report.json", "json");
            if (path.Length != 0)
            {
                m_ProjectReport.Save(path);
                UserPreferences.LoadSavePath = Path.GetDirectoryName(path);

                EditorUtility.RevealInFinder(path);
                AnalyticsReporter.SendEvent(AnalyticsReporter.UIButton.Save, AnalyticsReporter.BeginAnalytic());
            }

            GUIUtility.ExitGUI();
        }

        void LoadReport()
        {
            var path = EditorUtility.OpenFilePanel(k_LoadFromFile, UserPreferences.LoadSavePath, "json");
            if (path.Length != 0)
            {
                m_ProjectReport = ProjectReport.Load(path);
                if (m_ProjectReport.NumTotalIssues == 0)
                {
                    EditorUtility.DisplayDialog(k_LoadFromFile, k_LoadingFailed, "Ok");
                    return;
                }

                m_ProjectAuditor = new ProjectAuditor();

                m_LoadButtonAnalytic =  AnalyticsReporter.BeginAnalytic();
                m_AnalysisState = AnalysisState.Valid;
                UserPreferences.LoadSavePath = Path.GetDirectoryName(path);
                m_ViewManager = null; // make sure ViewManager is reinitialized

                OnEnable();

                UpdateAssemblyNames();
                UpdateAssemblySelection();

                // switch to summary view after loading
                m_ViewManager.ChangeView(IssueCategory.Metadata);
            }

            GUIUtility.ExitGUI();
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
                Debug.LogError($"Could not find Preferences for 'Analysis/{ProjectAuditor.DisplayName}'");
            }
        }

        [MenuItem("Window/Analysis/" + ProjectAuditor.DisplayName)]
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
            public static readonly GUIContent WindowTitle = new GUIContent(ProjectAuditor.DisplayName);

            public static readonly GUIContent AnalyzeButton =
                new GUIContent("Analyze", "Analyze Project and list all issues found.");
            public static readonly GUIContent ProjectAreaSelection =
                new GUIContent("Project Areas", $"Select project areas to analyze.");
            public static readonly GUIContent PlatformSelection =
                new GUIContent("Platform", "Select the target platform.");
            public static readonly GUIContent CompilationModeSelection =
                new GUIContent("Compilation Mode", "Select the compilation mode.");

            public static readonly GUIContent SettingsTitle = new GUIContent("Settings");
            public static readonly GUIContent NewSettingsButton = new GUIContent("Create New Settings");

            public static readonly GUIContent SaveButton = Utility.GetIcon(Utility.IconType.Save, "Save current report to json file");
            public static readonly GUIContent LoadButton = Utility.GetIcon(Utility.IconType.Load, "Load report from json file");
            public static readonly GUIContent DiscardButton = Utility.GetIcon(Utility.IconType.Trash, "Discard the current report.");

            public static readonly GUIContent HelpButton = Utility.GetIcon(Utility.IconType.Help, "Open Manual (in a web browser)");
            public static readonly GUIContent PreferencesMenuItem = EditorGUIUtility.TrTextContent("Preferences", $"Open User Preferences for {ProjectAuditor.DisplayName}");

            public static readonly GUIContent AssemblyFilter =
                new GUIContent("Assembly : ", "Select assemblies to examine");

            public static readonly GUIContent AssemblyFilterSelect =
                new GUIContent("Select", "Select assemblies to examine");

            public static readonly GUIContent AreaFilter =
                new GUIContent("Areas : ", "Select performance areas to display");

            public static readonly GUIContent AreaFilterSelect =
                new GUIContent("Select", "Select performance areas to display");

            public static readonly GUIContent FiltersFoldout = new GUIContent("Filters", "Filtering Criteria");

            public static readonly GUIContent WelcomeTextTitle = new GUIContent($"Welcome to {ProjectAuditor.DisplayName}");

            public static readonly GUIContent WelcomeText = new GUIContent(
                $@"
{ProjectAuditor.DisplayName} is a static analysis tool that analyzes assets, settings, and scripts of the Unity project and produces a report that contains the following:

 •  <b>Diagnostics</b>: a list of possible problems that might affect performance, memory and other areas.
 •  <b>BuildReport</b>: timing and size information of the last build.
 •  <b>Assets information</b>

To Analyze the project, click on <b>Analyze</b>.

Once the project is analyzed, {ProjectAuditor.DisplayName} displays a summary with high-level information. Then, it is possible to dive into a specific section of the report from the View menu.

A view allows the user to browse through the listed items and filter by string or other search criteria.
"
            );

            public static readonly GUIContent ConfigurationsTitle = new GUIContent("Configurations");

            public static readonly GUIContent Clear = new GUIContent("Clear");
            public static readonly GUIContent Refresh = new GUIContent("Refresh");

            public static readonly GUIContent ShaderVariants = new GUIContent("Variants", "Inspect Shader Variants");

            public static readonly GUIContent Packages = new GUIContent("Packages", "Installed Packages");
            public static readonly GUIContent PackageDiagnostics = new GUIContent("Diagnostics", "Package Diagnostics");
        }
    }
}
