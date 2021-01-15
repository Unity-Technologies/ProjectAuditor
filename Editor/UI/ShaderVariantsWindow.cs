using System;
using System.IO;
using System.Linq;
using Unity.ProjectAuditor.Editor.Auditors;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.UI
{
    class ShaderVariantsWindow : AnalysisWindow, IProjectIssueFilter
    {
        const string k_PlayerLogInfo = @"
To find which shader variants are compiled at runtime, follow these steps:
- Enable the Log Shader Compilation option (Project Settings => Graphics => Shader Loading)
- Build the project
- Run the build on the target platform
- Drag & Drop the Player.log file on this window
";
        const string k_NotLogFile = "Player log file not recognized.";
        const string k_NoCompiledVariantWarning = "No compiled shader variants found in player log. Make sure to enable Log Shader Compilation before building the project.";
        const string k_PlayerLogProcessed = "Player log file successfully processed.";

        bool m_HideCompiledVariants = false;
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

            var variants = m_Issues.Where(i => i.category == IssueCategory.ShaderVariants).ToArray();

            m_ShadersAuditor.ParsePlayerLog(logFilename, variants, new ProgressBarDisplay());

            var numCompiledVariants = variants.Count(i => i.GetCustomPropertyAsBool((int)ShaderVariantProperty.Compiled));
            if (numCompiledVariants == 0)
            {
                EditorUtility.DisplayDialog("Shader Variants", k_NoCompiledVariantWarning, "Ok");
            }
            else
            {
                m_AnalysisView.Refresh();
            }
        }

        public override void CreateTable(AnalysisViewDescriptor desc, ProjectAuditorConfig config, Preferences prefs, IProjectIssueFilter filter)
        {
            m_MainFilter = filter;
            base.CreateTable(desc, config, prefs, this);
        }

        public override void OnGUI()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);

            var helpStyle = new GUIStyle(EditorStyles.textField);
            helpStyle.wordWrap = true;
            EditorGUILayout.LabelField(k_PlayerLogInfo, helpStyle);
            var hideCompiledVariants = EditorGUILayout.ToggleLeft("Hide Compiled Variants", m_HideCompiledVariants, GUILayout.Width(160));
            if (hideCompiledVariants != m_HideCompiledVariants)
            {
                m_HideCompiledVariants = hideCompiledVariants;

                m_AnalysisView.Refresh();
            }

            EditorGUILayout.EndVertical();

            base.OnGUI();

            if (Event.current.type == EventType.DragExited)
            {
                HandleDragAndDrop();
            }
        }

        void HandleDragAndDrop()
        {
            var paths = DragAndDrop.paths;
            foreach (var path in paths)
            {
                if (Path.HasExtension(path) && Path.GetExtension(path).Equals(".log"))
                {
                    ParsePlayerLog(path);
                }
                else
                {
                    EditorUtility.DisplayDialog("Shader Variants", k_NotLogFile, "Ok");
                }
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
