using System;
using System.Collections.Generic;
using System.Linq;
using Unity.ProjectAuditor.Editor.UI.Framework;
using Unity.ProjectAuditor.Editor.AssemblyUtils;
using Unity.ProjectAuditor.Editor.Modules;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.UI
{
    class CompilerMessagesView : AnalysisView
    {
        const string k_Info = @"This view shows compiler error, warning and info messages.

To view Roslyn Analyzer diagnostics, make sure Roslyn Analyzer DLLs use the <b>RoslynAnalyzer</b> label.";
        const string k_RoslynDisabled = "The UseRoslynAnalyzers option is disabled. To enable Roslyn diagnostics reporting, make sure the corresponding option is enabled in the ProjectAuditor config.";
        const string k_NotAvailable = "This view is not available when 'CompilationMode' is set to 'CompilationMode.Editor'.";

        CompilationMode m_CompilationMode = CompilationMode.Player;
        bool m_RoslynAnalysis = false;

        public CompilerMessagesView(ViewManager viewManager) : base(viewManager)
        {
        }

        public override void AddIssues(IEnumerable<ProjectIssue> allIssues)
        {
            base.AddIssues(allIssues);
            var metaData = allIssues.FirstOrDefault(i => i.category == IssueCategory.MetaData && i.description.Equals(MetaDataModule.k_KeyCompilationMode));
            if (metaData != null)
                m_CompilationMode = (CompilationMode)Enum.Parse(typeof(CompilationMode), metaData.GetCustomProperty(MetaDataProperty.Value));
            metaData = allIssues.FirstOrDefault(i => i.category == IssueCategory.MetaData && i.description.Equals(MetaDataModule.k_KeyRoslynAnalysis));
            if (metaData != null)
                m_RoslynAnalysis = metaData.GetCustomPropertyAsBool(MetaDataProperty.Value);
        }

        protected override void DrawInfo()
        {
            EditorGUILayout.LabelField(k_Info, SharedStyles.TextArea);

            if (m_CompilationMode == CompilationMode.Editor)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.HelpBox(k_NotAvailable, MessageType.Warning);
                EditorGUILayout.EndHorizontal();
            }
            if (!m_RoslynAnalysis)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.HelpBox(k_RoslynDisabled, MessageType.Info);
                EditorGUILayout.EndHorizontal();
            }
        }
    }
}
