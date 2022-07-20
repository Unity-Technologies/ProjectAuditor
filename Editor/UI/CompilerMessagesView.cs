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
        const string k_Info = "This view shows the compiler error, warning and info messages.";
        const string k_NotAvailable = "This view is not available when 'CompilationMode' is set to 'CompilationMode.Editor'.";

        CompilationMode m_CompilationMode = CompilationMode.Player;

        public CompilerMessagesView(ViewManager viewManager) : base(viewManager)
        {
        }

        public override void AddIssues(IEnumerable<ProjectIssue> allIssues)
        {
            base.AddIssues(allIssues);
            var metaData = allIssues.FirstOrDefault(i => i.category == IssueCategory.MetaData && i.description.Equals(MetaDataModule.k_KeyCompilationMode));
            if (metaData != null)
                m_CompilationMode = (CompilationMode)Enum.Parse(typeof(CompilationMode), metaData.GetCustomProperty(MetaDataProperty.Value));
        }

        protected override void OnDrawInfo()
        {
            EditorGUILayout.LabelField(k_Info);
            if (m_CompilationMode == CompilationMode.Editor)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.HelpBox(k_NotAvailable, MessageType.Warning);
                EditorGUILayout.EndHorizontal();
            }
        }
    }
}
