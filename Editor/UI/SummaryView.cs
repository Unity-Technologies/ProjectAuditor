using System.Collections.Generic;
using System.Linq;
using Unity.ProjectAuditor.Editor.Diagnostic;
using Unity.ProjectAuditor.Editor.UI.Framework;
using Unity.ProjectAuditor.Editor.Modules;
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

        public override string description => $"Summary of the analysis.";

        public SummaryView(ViewManager viewManager) : base(viewManager)
        {
        }

        public override void AddIssues(IEnumerable<ProjectIssue> allIssues)
        {
            base.AddIssues(allIssues);

            m_Stats.numBuildSteps += allIssues.Count(i => i.category == IssueCategory.BuildStep);
            m_Stats.numCodeIssues += allIssues.Count(i => i.category == IssueCategory.Code);
            m_Stats.numSettingIssues += allIssues.Count(i => i.category == IssueCategory.ProjectSetting);
            m_Stats.numAssetIssues += allIssues.Count(i => i.category == IssueCategory.AssetDiagnostic);
            m_Stats.numShaders += allIssues.Count(i => i.category == IssueCategory.Shader);
            m_Stats.numPackages += allIssues.Count(i => i.category == IssueCategory.Package);
            m_Stats.numPackageDiagnostics += allIssues.Count(i => i.category == IssueCategory.PackageDiagnostic);

            var compilerMessages = allIssues.Where(i => i.category == IssueCategory.CodeCompilerMessage);
            m_Stats.numCompilerErrors += compilerMessages.Count(i => i.severity == Severity.Error);

            m_Stats.numCompiledAssemblies += allIssues.Count(i => i.category == IssueCategory.Assembly && i.severity != Severity.Error);
            m_Stats.numTotalAssemblies += allIssues.Count(i => i.category == IssueCategory.Assembly);
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
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical();

            EditorGUILayout.LabelField("Session Information", SharedStyles.BoldLabel);
            EditorGUI.indentLevel++;

            // note that m_Issues might change during background analysis.
            int itemIndex = 0;
            foreach (var issue in m_Issues.ToArray())
            {
                DrawKeyValue(issue.description, issue.GetCustomProperty(MetaDataProperty.Value), itemIndex++);
            }
            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();
        }

        void DrawKeyValue(string key, string value, int itemIndex)
        {
            EditorGUILayout.BeginHorizontal(itemIndex % 2 == 0 ? SharedStyles.Row : SharedStyles.RowAlternate);
            EditorGUILayout.LabelField($"{key}:", SharedStyles.Label, GUILayout.Width(160));
            EditorGUILayout.LabelField(value, SharedStyles.Label,  GUILayout.ExpandWidth(true));
            EditorGUILayout.EndHorizontal();
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
#if UNITY_2019_2_OR_NEWER
                if (GUILayout.Button(valueAsString, SharedStyles.LinkLabel))
                    m_ViewManager.ChangeView(category);
#else
                EditorGUILayout.LabelField(valueAsString, GUILayout.MaxWidth(90), GUILayout.ExpandWidth(false));
                if (GUILayout.Button("View", EditorStyles.miniButton, GUILayout.Width(50)))
                    m_ViewManager.ChangeView(category);
#endif
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
