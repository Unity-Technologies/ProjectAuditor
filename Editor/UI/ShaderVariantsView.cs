using System;
using System.IO;
using System.Linq;
using Unity.ProjectAuditor.Editor.Auditors;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.UI
{
    class ShaderVariantsView : AnalysisView, IProjectIssueFilter
    {
        const string k_BuildRequiredInfo =
@"- To view the built Shader Variants, run your build pipeline
- To update the view after building project and/or AssetBundles, use the Refresh button";

        const string k_PlayerLogInstructions =
@"This view shows the built Shader Variants.

The number of Variants contributes to the build size, however, there might be Variants that are not required (compiled) at runtime on the target platform. To find out which of these variants are not compiled at runtime, follow these steps:
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
        const string k_PlayerLogReadError = "Player log file could not be opened.";

        bool m_ShowCompiledVariants = true;
        bool m_ShowUncompiledVariants = true;
        IProjectIssueFilter m_MainFilter;
        ShadersAuditor m_ShadersAuditor;

        public void SetShadersAuditor(ShadersAuditor shadersAuditor)
        {
            m_ShadersAuditor = shadersAuditor;
        }

        void ParsePlayerLog(string logFilename)
        {
            if (string.IsNullOrEmpty(logFilename))
                return;

            const string dialogTitle = "Shader Variants";
            var variants = GetIssues().Where(i => i.category == IssueCategory.ShaderVariants).ToArray();
            var result = m_ShadersAuditor.ParsePlayerLog(logFilename, variants, new ProgressBarDisplay());
            switch (result)
            {
                case ParseLogResult.Success :
                    EditorUtility.DisplayDialog(dialogTitle, k_PlayerLogProcessed, "Ok");
                    Refresh();
                    break;
                case ParseLogResult.NoCompiledVariants :
                    if (GraphicsSettingsHelper.logShaderCompilationSupported)
                    {
                        if (GraphicsSettingsHelper.logWhenShaderIsCompiled)
                        {
                            EditorUtility.DisplayDialog(dialogTitle, k_NoCompiledVariantWarning, "Ok");
                        }
                        else
                        {
                            GraphicsSettingsHelper.logWhenShaderIsCompiled = EditorUtility.DisplayDialog(dialogTitle, k_NoCompiledVariantWarningLogDisabled, "Yes", "No");
                        }
                    }
                    break;
                case ParseLogResult.ReadError:
                    EditorUtility.DisplayDialog(dialogTitle, k_PlayerLogReadError, "Ok");
                    break;
            }
        }

        public override void Create(ViewDescriptor desc, IssueLayout layout, ProjectAuditorConfig config, Preferences prefs, IProjectIssueFilter filter)
        {
            m_MainFilter = filter;
            base.Create(desc, layout, config, prefs, this);
        }

        public override void DrawFilters()
        {
            GUI.enabled = numIssues > 0;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Extra :", GUILayout.Width(80));
            EditorGUI.BeginChangeCheck();
            m_ShowCompiledVariants = EditorGUILayout.ToggleLeft("Compiled Variants", m_ShowCompiledVariants, GUILayout.Width(160));
            m_ShowUncompiledVariants = EditorGUILayout.ToggleLeft("Uncompiled Variants", m_ShowUncompiledVariants, GUILayout.Width(160));
            if (EditorGUI.EndChangeCheck())
            {
                Refresh();
            }
            EditorGUILayout.EndHorizontal();

            GUI.enabled = true;
        }

        protected override void OnDrawInfo()
        {
            var variantsAvailable = numIssues > 0;

            EditorGUILayout.BeginVertical(GUI.skin.box);

            if (variantsAvailable)
            {
                EditorGUILayout.LabelField(GraphicsSettingsHelper.logShaderCompilationSupported
                    ? k_PlayerLogInstructions
                    : k_PlayerLogParsingUnsupported, SharedStyles.TextArea);

                if (GraphicsSettingsHelper.logShaderCompilationSupported)
                    GraphicsSettingsHelper.logWhenShaderIsCompiled = EditorGUILayout.Toggle("Log Shader Compilation (requires Build&Run)", GraphicsSettingsHelper.logWhenShaderIsCompiled, GUILayout.Width(320));

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
            else
            {
                EditorGUILayout.LabelField(k_BuildRequiredInfo, SharedStyles.TextArea);
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

        public bool Match(ProjectIssue issue)
        {
            if (!m_MainFilter.Match(issue))
                return false;
            var compiled = issue.GetCustomPropertyAsBool((int)ShaderVariantProperty.Compiled);
            if (compiled && m_ShowCompiledVariants)
                return true;
            if (!compiled && m_ShowUncompiledVariants)
                return true;
            return false;
        }
    }
}
