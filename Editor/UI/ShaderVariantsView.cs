using System;
using System.IO;
using System.Linq;
using Unity.ProjectAuditor.Editor.UI.Framework;
using Unity.ProjectAuditor.Editor.Auditors;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.UI
{
    class ShaderVariantsView : AnalysisView
    {
        const string k_BuildInstructions =
@"This view shows the built Shader Variants.

To view the built Shader Variants, run your build pipeline and Refresh:
- Build the project and/or Addressables/AssetBundles
- Click the Refresh button
Note that it's important to clear the cache before building Addressables.

To clear the recorded variants use the Clear button";

        const string k_PlayerLogInstructions =
@"The number of Variants contributes to the build size, however, there might be Variants that are not required (compiled) at runtime on the target platform. To find out which of these variants are not compiled at runtime, follow these steps:
- Enable the Log Shader Compilation option
- Make a Development build
- Run the build on the target platform. Make sure to go through all scenes.
- Drag & Drop the Player.log file on this window";

        const string k_PlayerLogParsingUnsupported =
@"This view shows the built Shader Variants.

The number of Variants contributes to the build size, however, there might be Variants that are not required (compiled) at runtime on the target platform. To find out which of these variants are not compiled at runtime, update to the latest Unity 2018+ LTS.";

        const string k_NoCompiledVariantWarning = "No compiled shader variants found in player log. Perhaps, Log Shader Compilation was not enabled when the project was built.";
        const string k_NoCompiledVariantWarningLogDisabled = "No compiled shader variants found in player log. Shader compilation logging is disabled. Would you like to enable it? (Shader compilation will not appear in the log until the project is rebuilt)";
        const string k_PlayerLogProcessed = "Player log file successfully processed.";
        const string k_PlayerLogReadError = "Player log file could not be opened. Make sure the Player application has been closed.";

        bool m_ShowCompiledVariants = true;
        bool m_ShowUncompiledVariants = true;

        void ParsePlayerLog(string logFilename)
        {
            if (string.IsNullOrEmpty(logFilename))
                return;

            const string dialogTitle = "Shader Variants";
            var variants = GetIssues().Where(i => i.category == IssueCategory.ShaderVariant).ToArray();
            var result = ShadersModule.ParsePlayerLog(logFilename, variants, new ProgressBar());
            switch (result)
            {
                case ParseLogResult.Success:
                    EditorUtility.DisplayDialog(dialogTitle, k_PlayerLogProcessed, "Ok");
                    Refresh();
                    break;
                case ParseLogResult.NoCompiledVariants:
                    if (GraphicsSettingsProxy.logShaderCompilationSupported)
                    {
                        if (GraphicsSettingsProxy.logWhenShaderIsCompiled)
                        {
                            EditorUtility.DisplayDialog(dialogTitle, k_NoCompiledVariantWarning, "Ok");
                        }
                        else
                        {
                            GraphicsSettingsProxy.logWhenShaderIsCompiled = EditorUtility.DisplayDialog(dialogTitle, k_NoCompiledVariantWarningLogDisabled, "Yes", "No");
                        }
                    }
                    break;
                case ParseLogResult.ReadError:
                    EditorUtility.DisplayDialog(dialogTitle, k_PlayerLogReadError, "Ok");
                    break;
            }
        }

        public override void DrawFilters()
        {
            GUI.enabled = numIssues > 0;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Extra :", GUILayout.Width(80));
            EditorGUI.BeginChangeCheck();
            m_ShowCompiledVariants = EditorGUILayout.ToggleLeft("Compiled Variants", m_ShowCompiledVariants, GUILayout.Width(180));
            m_ShowUncompiledVariants = EditorGUILayout.ToggleLeft("Uncompiled Variants", m_ShowUncompiledVariants, GUILayout.Width(180));
            if (EditorGUI.EndChangeCheck())
            {
                Refresh();
            }
            EditorGUILayout.EndHorizontal();

            GUI.enabled = true;
        }

        protected override void OnDrawInfo()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField(k_BuildInstructions, SharedStyles.TextArea);

            if (numIssues > 0)
            {
                EditorGUILayout.LabelField(GraphicsSettingsProxy.logShaderCompilationSupported
                    ? k_PlayerLogInstructions
                    : k_PlayerLogParsingUnsupported, SharedStyles.TextArea);

                if (GraphicsSettingsProxy.logShaderCompilationSupported)
                    GraphicsSettingsProxy.logWhenShaderIsCompiled = EditorGUILayout.Toggle("Log Shader Compilation (requires Build&Run)", GraphicsSettingsProxy.logWhenShaderIsCompiled, GUILayout.Width(320));

                var evt = Event.current;

                switch (evt.type)
                {
                    case EventType.DragExited:
                        break;
                    case EventType.DragUpdated:
                        var valid = 1 == DragAndDrop.paths.Count(path => Path.HasExtension(path) && Path.GetExtension(path).Equals(".log"));
                        DragAndDrop.visualMode = valid ? DragAndDropVisualMode.Generic : DragAndDropVisualMode.Rejected;
                        evt.Use();
                        break;
                    case EventType.DragPerform:
                        DragAndDrop.AcceptDrag();
                        HandleDragAndDrop();
                        evt.Use();
                        break;
                }
            }

            EditorGUILayout.EndVertical();
        }

        void HandleDragAndDrop()
        {
            var paths = DragAndDrop.paths;
            foreach (var path in paths)
            {
                ParsePlayerLog(path);
            }
        }

        public override bool Match(ProjectIssue issue)
        {
            if (!base.Match(issue))
                return false;
            var compiled = issue.GetCustomPropertyAsBool(ShaderVariantProperty.Compiled);
            if (compiled && m_ShowCompiledVariants)
                return true;
            if (!compiled && m_ShowUncompiledVariants)
                return true;
            return false;
        }

        public ShaderVariantsView(ViewManager viewManager) : base(viewManager)
        {
        }
    }
}
