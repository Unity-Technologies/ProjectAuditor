using Unity.ProjectAuditor.Editor.UI.Framework;
using UnityEditor;

namespace Unity.ProjectAuditor.Editor.UI
{
    internal class CodeDomainReloadView : CodeDiagnosticView
    {
        const string k_RoslynDisabled = @"The UseRoslynAnalyzers option is disabled. This is required to see results from the Domain Reload Analyzer.

To enable Roslyn diagnostics reporting, make sure the corresponding option is enabled in Preferences > Analysis > " + ProjectAuditor.DisplayName + @" > Use Roslyn Analyzers.
To open the Preferences window, go to Edit > Preferences (macOS: Unity > Settings) in the main menu.";

        public CodeDomainReloadView(ViewManager viewManager) : base(viewManager)
        {
        }

        protected override void DrawInfo()
        {
            if (!m_ViewManager.Report.SessionInfo.UseRoslynAnalyzers)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.HelpBox(k_RoslynDisabled, MessageType.Warning);
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                base.DrawInfo();
            }
        }

        public override void DrawFilters()
        {
        }
    }
}
