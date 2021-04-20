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
@"Build the project to view the Shader Variants";

        const string k_PlayerLogInstructions =
@"This view shows the built Shader Variants.

The number of Variants contributes to the build size, however, there might be Variants that are not required (compiled) at runtime on the target platform. To find out which of these variants are not compiled at runtime, follow these steps:
- Enable the Log Shader Compilation option (if not enabled)
- Make a Development build
- Run the build on the target platform. Make sure to go through all scenes.
- Drag & Drop the Player.log file on this window";

        const string k_PlayerLogParsingUnsupported =
@"This view shows the built Shader Variants.

The number of Variants contributes to the build size, however, there might be Variants that are not required (compiled) at runtime on the target platform. To find out which of these variants are not compiled at runtime, update to the latest Unity 2018+ LTS.";

        const string k_NoCompiledVariantWarning = "No compiled shader variants found in player log. Perhaps, Log Shader Compilation was not enabled when the project was built.";
        const string k_NoCompiledVariantWarningLogDisabled = "No compiled shader variants found in player log. Shader compilation logging is disabled. Would you like to enable it? (Shader compilation will not appear in the log until the project is rebuilt)";
        const string k_PlayerLogProcessed = "Player log file successfully processed.";

        bool m_HideCompiledVariants;
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

            var variants = GetIssues().Where(i => i.category == IssueCategory.ShaderVariants).ToArray();

            if (m_ShadersAuditor.ParsePlayerLog(logFilename, variants, new ProgressBarDisplay()))
            {
                EditorUtility.DisplayDialog("Shader Variants", k_PlayerLogProcessed, "Ok");
                Refresh();
            }
            else if (GraphicsSettingsHelper.logShaderCompilationSupported)
            {
                if (GraphicsSettingsHelper.logWhenShaderIsCompiled)
                {
                    EditorUtility.DisplayDialog("Shader Variants", k_NoCompiledVariantWarning, "Ok");
                }
                else
                {
                    GraphicsSettingsHelper.logWhenShaderIsCompiled = EditorUtility.DisplayDialog("Shader Variants", k_NoCompiledVariantWarningLogDisabled, "Yes", "No");
                }
            }
        }

        public override void Create(ViewDescriptor desc, IssueLayout layout, ProjectAuditorConfig config, Preferences prefs, IProjectIssueFilter filter)
        {
            m_MainFilter = filter;
            base.Create(desc, layout, config, prefs, this);
        }

        public override void DrawFilters()
        {
            var lastEnabled = GUI.enabled;
            GUI.enabled = numIssues > 0;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Extra :", GUILayout.Width(80));
            EditorGUI.BeginChangeCheck();
            m_HideCompiledVariants = EditorGUILayout.ToggleLeft("Hide Compiled Variants", m_HideCompiledVariants, GUILayout.Width(180));
            if (EditorGUI.EndChangeCheck())
            {
                Refresh();
            }

            EditorGUILayout.EndHorizontal();

            GUI.enabled = lastEnabled;
        }

        protected override void OnDrawInfo()
        {
            var variantsAvailable = numIssues > 0;

            EditorGUILayout.BeginVertical(GUI.skin.box);

            var textArea = new GUIStyle(EditorStyles.textArea); // TEMP

            if (variantsAvailable)
            {
                EditorGUILayout.LabelField(GraphicsSettingsHelper.logShaderCompilationSupported
                    ? k_PlayerLogInstructions
                    : k_PlayerLogParsingUnsupported, textArea);

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
                EditorGUILayout.LabelField(k_BuildRequiredInfo, textArea);
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
            if (m_HideCompiledVariants)
                return !issue.GetCustomPropertyAsBool((int)ShaderVariantProperty.Compiled);
            return true;
        }
    }
}
