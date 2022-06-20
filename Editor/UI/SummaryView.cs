using System;
using System.Collections.Generic;
using System.Linq;
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
            public int numResources;
            public int numShaders;
        }

        Stats m_Stats;

        public SummaryView(ViewManager viewManager) : base(viewManager)
        {
        }

        public override void AddIssues(IEnumerable<ProjectIssue> allIssues)
        {
            base.AddIssues(allIssues);

            m_Stats.numBuildSteps += allIssues.Count(i => i.category == IssueCategory.BuildStep);
            m_Stats.numCodeIssues += allIssues.Count(i => i.category == IssueCategory.Code);
            m_Stats.numSettingIssues += allIssues.Count(i => i.category == IssueCategory.ProjectSetting);
            m_Stats.numResources += allIssues.Count(i => i.category == IssueCategory.Asset);
            m_Stats.numShaders += allIssues.Count(i => i.category == IssueCategory.Shader);

            var compilerMessages = allIssues.Where(i => i.category == IssueCategory.CodeCompilerMessage);
            m_Stats.numCompilerErrors += compilerMessages.Count(i => i.severity == Rule.Severity.Error);

            m_Stats.numCompiledAssemblies += allIssues.Count(i => i.category == IssueCategory.Assembly && i.severity != Rule.Severity.Error);
            m_Stats.numTotalAssemblies += allIssues.Count(i => i.category == IssueCategory.Assembly);
        }

        public override void Clear()
        {
            base.Clear();

            m_Stats = new Stats();
        }

        protected override void OnDrawInfo()
        {
            EditorGUILayout.LabelField("Analysis overview", EditorStyles.boldLabel);

            EditorGUI.indentLevel++;
            if (m_Stats.numCodeIssues > 0)
                DrawSummaryItem("Code Issues: ", m_Stats.numCodeIssues, IssueCategory.Code);
            if (m_Stats.numCompiledAssemblies > 0)
                DrawSummaryItem("Compiled Assemblies: ", string.Format("{0} / {1}", m_Stats.numCompiledAssemblies, m_Stats.numTotalAssemblies), IssueCategory.Assembly);
            if (m_Stats.numCompilerErrors > 0)
            {
                DrawSummaryItem("Compilation Errors: ", m_Stats.numCompilerErrors, IssueCategory.CodeCompilerMessage, Utility.GetSeverityIcon(Rule.Severity.Error));
            }
            if (m_Stats.numSettingIssues > 0)
                DrawSummaryItem("Settings Issues:", m_Stats.numSettingIssues, IssueCategory.ProjectSetting);
            if (m_Stats.numResources > 0)
                DrawSummaryItem("Assets in Resources folders:", m_Stats.numResources, IssueCategory.Asset);
            if (m_Stats.numShaders > 0)
                DrawSummaryItem("Shaders in the project:", m_Stats.numShaders, IssueCategory.Shader);
            var buildAvailable = m_Stats.numBuildSteps > 0;
            DrawSummaryItem("Build Report available:", buildAvailable, IssueCategory.BuildStep);
            EditorGUI.indentLevel--;

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Select a View from the toolbar to start browsing the report");
        }

        public override void DrawContent()
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical();
            EditorGUI.indentLevel++;

            // note that m_Issues might change during background analysis.
            foreach (var issue in m_Issues.ToArray())
            {
                DrawKeyValue(issue.description, issue.GetCustomProperty(MetaDataProperty.Value));
            }
            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();
        }

        void DrawKeyValue(string key, string value)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(string.Format("{0}:", key), GUILayout.ExpandWidth(false));
            EditorGUILayout.LabelField(value,  GUILayout.ExpandWidth(true));
            EditorGUILayout.EndHorizontal();
        }

        void DrawSummaryItem<T>(string title, T value, IssueCategory category, GUIContent icon = null)
        {
            var viewLink = true;
            var valueAsString = value.ToString();
            if (typeof(T) == typeof(bool))
            {
                var valueAsBool = (bool)(object)value;
                valueAsString = valueAsBool ? "Yes" : "No";
                viewLink = valueAsBool;
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(title, GUILayout.ExpandWidth(false));

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
            }
            if (icon != null)
                EditorGUILayout.LabelField(icon);
            EditorGUILayout.EndHorizontal();
        }
    }
}
