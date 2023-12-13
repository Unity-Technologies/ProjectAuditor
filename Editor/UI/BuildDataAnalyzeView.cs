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

        const int k_MaxLabelWidth = 500;

        string m_LastBuildDataPath;
        string m_ShortenedPath;

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

                    using (new EditorGUILayout.VerticalScope(GUILayout.Width(k_MaxLabelWidth)))
                    {
                        EditorGUILayout.LabelField(Contents.SettingsContent, SharedStyles.BoldLabel);

                        GUILayout.Space(5);

                        EditorGUILayout.LabelField(Contents.BuildDataFolderContent, SharedStyles.BoldLabel);
                        EditorGUILayout.LabelField(new GUIContent(m_ShortenedPath, m_LastBuildDataPath));
                        var changeFolder = GUILayout.Button(Contents.ChangeFolderContent, GUILayout.Width(200));

                        if (changeFolder)
                        {
                            m_LastBuildDataPath = EditorUtility.OpenFolderPanel(Contents.ChoseFolderText,
                                m_LastBuildDataPath, "");

                            m_ShortenedPath = ShortenPath(m_LastBuildDataPath, GUI.skin.label, k_MaxLabelWidth - 10);
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

            m_ShortenedPath = ShortenPath(m_LastBuildDataPath, GUI.skin.label, k_MaxLabelWidth - 10);

            m_Initialized = true;
        }

        public string ShortenPath(string path, GUIStyle style, float maxWidth)
        {
            if (style.CalcSize(new GUIContent(path)).x <= maxWidth) return path;

            string[] parts = path.Split('/', '\\');
            float totalWidth = style.CalcSize(new GUIContent(path)).x;
            float ellipsisWidth = style.CalcSize(new GUIContent("...")).x;

            if ((totalWidth - parts.Length + 1 + ellipsisWidth) <= maxWidth) return path;

            int middleIndex = parts.Length / 2;

            // Keep removing folders from the middle, then continue to the left, until the path is short enough
            while (totalWidth > maxWidth && middleIndex > 0)
            {
                if (parts[middleIndex] != "...")
                {
                    totalWidth = totalWidth - style.CalcSize(new GUIContent(parts[middleIndex])).x + ellipsisWidth;
                    parts[middleIndex] = "...";
                }
                else if (parts[middleIndex - 1] != "...")
                {
                    totalWidth -= style.CalcSize(new GUIContent(parts[middleIndex - 1])).x;
                    parts[middleIndex - 1] = "...";
                }
                else if (parts[middleIndex + 1] != "...")
                {
                    totalWidth -= style.CalcSize(new GUIContent(parts[middleIndex + 1])).x;
                    parts[middleIndex + 1] = "...";
                }

                middleIndex--;
            }

            return string.Join("/", parts);
        }
    }
}
