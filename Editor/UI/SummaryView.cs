using System.Collections.Generic;
using System.Linq;
using Unity.ProjectAuditor.Editor.Diagnostic;
using Unity.ProjectAuditor.Editor.UI.Framework;
using Unity.ProjectAuditor.Editor.Modules;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.UI
{
    class SummaryView : AnalysisView
    {
        struct Stats
        {
            public int numBuildSteps;
            public int numCodeIssues;
            public int numCompiledAssemblies;
            public int numCompilerErrors;
            public int numSettingIssues;
            public int numTotalAssemblies;
            public int numAssetIssues;
            public int numShaders;
            public int numPackages;
            public int numPackageDiagnostics;
        }

        Stats m_Stats;

        public override string Description => $"Summary of the analysis.";

        public SummaryView(ViewManager viewManager) : base(viewManager)
        {
        }

        public override void AddIssues(IEnumerable<ProjectIssue> allIssues)
        {
            base.AddIssues(allIssues);

            m_Stats.numBuildSteps += allIssues.Count(i => i.Category == IssueCategory.BuildStep);
            m_Stats.numCodeIssues += allIssues.Count(i => i.Category == IssueCategory.Code);
            m_Stats.numSettingIssues += allIssues.Count(i => i.Category == IssueCategory.ProjectSetting);
            m_Stats.numAssetIssues += allIssues.Count(i => i.Category == IssueCategory.AssetDiagnostic);
            m_Stats.numShaders += allIssues.Count(i => i.Category == IssueCategory.Shader);
            m_Stats.numPackages += allIssues.Count(i => i.Category == IssueCategory.Package);
            m_Stats.numPackageDiagnostics += allIssues.Count(i => i.Category == IssueCategory.PackageDiagnostic);

            var compilerMessages = allIssues.Where(i => i.Category == IssueCategory.CodeCompilerMessage);
            m_Stats.numCompilerErrors += compilerMessages.Count(i => i.Severity == Severity.Error);

            m_Stats.numCompiledAssemblies += allIssues.Count(i => i.Category == IssueCategory.Assembly && i.Severity != Severity.Error);
            m_Stats.numTotalAssemblies += allIssues.Count(i => i.Category == IssueCategory.Assembly);
        }

        public override void Clear()
        {
            base.Clear();

            m_Stats = new Stats();
        }

        protected override void DrawInfo()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.Space();

                EditorGUILayout.BeginVertical(GUILayout.Width(300));
                EditorGUILayout.LabelField("Diagnostics", SharedStyles.BoldLabel);

                EditorGUI.indentLevel++;

                if (m_Stats.numCodeIssues > 0)
                    DrawSummaryItem("Code: ", m_Stats.numCodeIssues, IssueCategory.Code);
                if (m_Stats.numSettingIssues > 0)
                    DrawSummaryItem("Settings:", m_Stats.numSettingIssues, IssueCategory.ProjectSetting);
                if (m_Stats.numAssetIssues > 0)
                    DrawSummaryItem("Assets:", m_Stats.numAssetIssues, IssueCategory.AssetDiagnostic);
                if (m_Stats.numPackages > 0)
                    DrawSummaryItem("Packages:", m_Stats.numPackageDiagnostics, IssueCategory.PackageDiagnostic);

                DrawLine();
                EditorGUI.indentLevel--;
                EditorGUILayout.EndVertical();

                EditorGUILayout.Space();

                EditorGUILayout.BeginVertical(GUILayout.Width(300));
                EditorGUILayout.LabelField("Statistics", SharedStyles.BoldLabel);

                EditorGUI.indentLevel++;
                if (m_Stats.numCompilerErrors > 0)
                {
                    DrawSummaryItem("Compilation Errors: ", m_Stats.numCompilerErrors, IssueCategory.CodeCompilerMessage, Utility.GetIcon(Utility.IconType.Error));
                }
                var buildAvailable = m_Stats.numBuildSteps > 0;
                DrawSummaryItem("Build Report:", buildAvailable, IssueCategory.BuildStep);
                if (m_Stats.numCompiledAssemblies > 0)
                    DrawSummaryItem("Compiled Assemblies: ", string.Format("{0} / {1}", m_Stats.numCompiledAssemblies, m_Stats.numTotalAssemblies), IssueCategory.Assembly);
                if (m_Stats.numShaders > 0)
                    DrawSummaryItem("Shaders:", m_Stats.numShaders, IssueCategory.Shader);

                DrawLine();
                EditorGUI.indentLevel--;
                EditorGUILayout.EndVertical();

                GUILayout.FlexibleSpace();
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Select a View from the toolbar to start browsing the report");
        }

        public override void DrawContent(bool showDetails = false)
        {
            if (m_ViewManager.Report == null)
                return;

            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical();

            DrawSessionInfo(m_ViewManager.Report.SessionInfo);

            EditorGUILayout.EndVertical();
        }

        static void DrawSessionInfo(SessionInfo sessionInfo)
        {
            var keyValues = new[]
            {
                new KeyValuePair<string, string>("Date and Time", Formatting.FormatDateTime(Json.DeserializeDateTime(sessionInfo.DateTime))),
                new KeyValuePair<string, string>("Host Name", sessionInfo.HostName),
                new KeyValuePair<string, string>("Host Platform", sessionInfo.HostPlatform),
                new KeyValuePair<string, string>("Company Name", sessionInfo.CompanyName),
                new KeyValuePair<string, string>("Project Name", sessionInfo.ProjectName),
                new KeyValuePair<string, string>("Project Revision", sessionInfo.ProjectRevision),
                new KeyValuePair<string, string>("Unity Version", sessionInfo.UnityVersion),
                new KeyValuePair<string, string>("Project Auditor Version", sessionInfo.ProjectAuditorVersion)
            };

            DrawSessionFields("Session Information", keyValues);
        }

        static void DrawSessionFields(string label, KeyValuePair<string, string>[] keyValues)
        {
            EditorGUILayout.LabelField(label, SharedStyles.BoldLabel);
            EditorGUI.indentLevel++;

            var itemIndex = 0;
            foreach (var pair in keyValues)
            {
                EditorGUILayout.BeginHorizontal(itemIndex++ % 2 == 0 ? SharedStyles.Row : SharedStyles.RowAlternate);
                EditorGUILayout.LabelField($"{pair.Key}:", SharedStyles.Label, GUILayout.Width(160));
                EditorGUILayout.LabelField(pair.Value, SharedStyles.Label,  GUILayout.ExpandWidth(true));
                EditorGUILayout.EndHorizontal();
            }

            EditorGUI.indentLevel--;
        }

        void DrawSummaryItem<T>(string title, T value, IssueCategory category, GUIContent icon = null)
        {
            if (!m_ViewManager.HasView(category))
                return;

            var viewLink = true;
            var valueAsString = value.ToString();
            if (typeof(T) == typeof(bool))
            {
                var valueAsBool = (bool)(object)value;
                valueAsString = valueAsBool ? "Available" : "Not Available";
                viewLink = valueAsBool;
            }

            DrawLine();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(title, SharedStyles.Label, GUILayout.ExpandWidth(false));

            if (viewLink)
            {
                if (GUILayout.Button(valueAsString, SharedStyles.LinkLabel))
                    m_ViewManager.ChangeView(category);
            }
            else
            {
                EditorGUILayout.LabelField(valueAsString, GUILayout.MaxWidth(90), GUILayout.ExpandWidth(false));
                GUILayout.FlexibleSpace();
            }
            if (icon != null)
                EditorGUILayout.LabelField(icon, SharedStyles.Label);
            EditorGUILayout.EndHorizontal();
        }

        void DrawLine()
        {
            var rect = EditorGUILayout.GetControlRect(GUILayout.Height(1));
            var color = new Color(0.3f, 0.3f, 0.3f);

            if (m_2D.DrawStart(rect))
            {
                m_2D.DrawLine(0, 0, rect.width, 0, color);
                m_2D.DrawEnd();
            }
        }
    }
}
