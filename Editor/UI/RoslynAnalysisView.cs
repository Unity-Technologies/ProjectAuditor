using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.UI
{
    class RoslynAnalysisView : AnalysisView
    {
        const string k_Instructions = @"To use a Roslyn analyzer library:
- Add the Roslyn analyzers DLL to the project.
- In the Plugin Inspector, under Select platforms for plugin, disable Any Platform. Under Include platforms, disable Editor and Standalone platforms.
- Assign a new label called RoslynAnalyzer to the DLL.
- Re-run the Project Auditor Analysis";

        protected override void OnDrawInfo()
        {
            EditorGUILayout.LabelField(k_Instructions, Styles.TextArea);
        }
    }
}
