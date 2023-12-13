using System.IO;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor.Build;
using Unity.ProjectAuditor.Editor.BuildData;
using Unity.ProjectAuditor.Editor.UI.Framework;
using Unity.ProjectAuditor.Editor.UnityFileSystemApi;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.UI
{
    class BuildDataAnalyzeView : AnalysisView
    {
        public override string Description => "Build Data Analysis";

        internal static readonly IssueCategory[] k_BuildDataCategories = new IssueCategory[]
        {
            IssueCategory.BuildDataTexture2D, IssueCategory.BuildDataMesh,
            IssueCategory.BuildDataShader, IssueCategory.BuildDataShaderVariant,
            IssueCategory.BuildDataAnimationClip, IssueCategory.BuildDataAudioClip, IssueCategory.BuildDataSummary
        };

        static internal class Contents
        {
            public static GUIContent BuildDataFolderContent = new GUIContent("Build Data Folder:");
            public static GUIContent StartAnalysisButtonContent = new GUIContent("Start Build Data Analysis");
            public static GUIContent ChangeFolderContent = new GUIContent("Change Build Data Folder");
            public static GUIContent SettingsContent = new GUIContent("Settings");
            public static string ChoseFolderText = "Choose folder with built player data";
            public static string InfoText = "Analyze Build Data now? Check the Settings below before starting the analysis.";
        }

        string m_LastBuildDataPath;
        Analyzer m_BuildDataAnalyzer;
        ProjectAuditorWindow m_ProjectAuditorWindow;

        bool m_Initialized;

        public ProjectAuditorWindow ProjectAuditorWindow
        {
            set => m_ProjectAuditorWindow = value;
        }

        public BuildDataAnalyzeView(ViewManager viewManager) : base(viewManager)
        {
        }

        public override void DrawContent(bool showDetails = false)
        {
            Initialize();

            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                GUILayout.Space(10);

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.HelpBox(Contents.InfoText, MessageType.Info);
                    GUILayout.FlexibleSpace();
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();

                    using (new EditorGUILayout.VerticalScope())
                    {
                        GUILayout.Space(10);

                        GUI.enabled = !string.IsNullOrEmpty(m_LastBuildDataPath);
                        bool doRun = GUILayout.Button(Contents.StartAnalysisButtonContent, GUILayout.Width(200));
                        GUI.enabled = true;

                        if (doRun)
                        {
                            var progress = new ProgressBar();

                            progress.Start("Scanning Build Data", "In Progress...", 0);

                            UnityFileSystem.Init();

                            m_BuildDataAnalyzer = new Analyzer();
                            m_ProjectAuditorWindow.BuildObjects = m_BuildDataAnalyzer.Analyze(m_LastBuildDataPath, "*");

                            progress.Start("Starting Build Data Analysis", "In Progress...", 0);

                            m_ProjectAuditorWindow.AuditCategories(k_BuildDataCategories);

                            progress.Clear();
                        }
                    }

                    GUILayout.FlexibleSpace();
                }

                GUILayout.Space(10);

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();

                    using (new EditorGUILayout.VerticalScope(GUILayout.Width(600)))
                    {
                        EditorGUILayout.LabelField(Contents.SettingsContent, SharedStyles.BoldLabel);

                        GUILayout.Space(5);

                        EditorGUILayout.LabelField(Contents.BuildDataFolderContent, SharedStyles.BoldLabel);
                        EditorGUILayout.LabelField(m_LastBuildDataPath);
                        var changeFolder = GUILayout.Button(Contents.ChangeFolderContent, GUILayout.Width(200));

                        if (changeFolder)
                        {
                            m_LastBuildDataPath = EditorUtility.OpenFolderPanel(Contents.ChoseFolderText,
                                m_LastBuildDataPath, "");
                        }
                    }

                    GUILayout.FlexibleSpace();
                }

                GUILayout.Space(20);

                GUILayout.FlexibleSpace();
            }
        }

        private void Initialize()
        {
            if (m_Initialized)
                return;

            Assert.NotNull(m_ProjectAuditorWindow,
                "ProjectAuditorWindow wasn't set at creation time of BuildDataAnalyzeView. View won't work properly.");

            var provider = new LastBuildReportProvider();
            var buildReport = provider.GetBuildReport(m_ProjectAuditorWindow.Platform);
            m_LastBuildDataPath = buildReport != null
                ? Path.GetDirectoryName(buildReport.summary.outputPath)
                : "";

            m_Initialized = true;
        }
    }
}
