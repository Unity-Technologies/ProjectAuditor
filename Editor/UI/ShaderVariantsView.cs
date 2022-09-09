using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.UI.Framework;
using Unity.ProjectAuditor.Editor.Modules;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.UI
{
    class ShaderVariantsView : AnalysisView
    {
        const string k_BulletPointUnicode = " \u2022";

        static readonly string k_BuildInstructions = $@"This view shows the built Shader Variants.

To record and view the Shader Variants for this project, follow these steps:
{k_BulletPointUnicode} Click the <b>Clear</b> button
{k_BulletPointUnicode} Build the project and/or Addressables/AssetBundles
{k_BulletPointUnicode} Click the <b>Refresh</b> button";

        static readonly string k_ClearInstructions = "Depending on the Unity version, the incremental build pipeline might not recompile all variants if the project was built previously. Therefore it is important to Clear before building.";

        static readonly string k_PlayerLogInstructions = $@"The number of Variants contributes to the build size, however, there might be Variants that are not required (compiled) at runtime on the target platform. To find out which of these variants are not compiled at runtime, follow these steps:
{k_BulletPointUnicode} Enable the <b>Log Shader Compilation</b> option
{k_BulletPointUnicode} Make a Development build
{k_BulletPointUnicode} Run the build on the target platform. Make sure to go through all scenes.
{k_BulletPointUnicode} Drag & Drop the Player.log file on this window";

        const string k_PlayerLogParsingDialogTitle = "Shader Variants";

        const string k_PlayerLogParsingUnsupported = "To find out which of these variants are not compiled at runtime, update to the latest Unity 2018+ LTS.";

        const string k_NoCompiledVariantWarning = "No compiled shader variants found in player log. Perhaps, Log Shader Compilation was not enabled when the project was built.";
        const string k_NoCompiledVariantWarningLogDisabled = "No compiled shader variants found in player log. Shader compilation logging is disabled. Would you like to enable it? (Shader compilation will not appear in the log until the project is rebuilt)";
        const string k_PlayerLogProcessed = "Player log file successfully processed.";
        const string k_PlayerLogReadError = "Player log file could not be opened. Make sure the Player application has been closed.";
        const string k_Ok = "Ok";
        const string k_Yes = "Yes";
        const string k_No = "No";
        const string k_LogShaderCompilation = "Log Shader Compilation (requires Build&Run)";
        const string k_ExportAsVariantCollection = "Export as Shader Variant Collection";

        bool m_ExportAsVariantCollection = true;
        bool m_ShowCompiledVariants = true;
        bool m_ShowUncompiledVariants = true;

        struct PropertyFoldout
        {
            public int id;
            public bool enabled;
            public GUIContent content;
            public Vector2 scroll;
        }

        PropertyFoldout[] m_PropertyFoldouts;

        public override void Create(ViewDescriptor descriptor, IssueLayout layout, ProjectAuditorConfig config,
            ProjectAuditorModule module, ViewStates viewStates, IProjectIssueFilter filter)
        {
            var propertyFoldouts = new List<PropertyFoldout>();

            propertyFoldouts.Add(new PropertyFoldout
            {
                id = descriptor.category == IssueCategory.ShaderVariant ? (int)ShaderVariantProperty.Keywords : (int)ComputeShaderVariantProperty.Keywords,
                enabled = true,
                content = new GUIContent("Keywords")
            });
            propertyFoldouts.Add(new PropertyFoldout
            {
                id = descriptor.category == IssueCategory.ShaderVariant ? (int)ShaderVariantProperty.PlatformKeywords : (int)ComputeShaderVariantProperty.PlatformKeywords,
                enabled = true,
                content = new GUIContent("Platform Keywords")
            });

            if (descriptor.category == IssueCategory.ShaderVariant)
                propertyFoldouts.Add(new PropertyFoldout
                {
                    id = (int)ShaderVariantProperty.Requirements,
                    enabled = true,
                    content = new GUIContent("Requirements")
                });
            m_PropertyFoldouts = propertyFoldouts.ToArray();

            base.Create(descriptor, layout, config, module, viewStates, filter);
        }

        void ParsePlayerLog(string logFilename)
        {
            if (string.IsNullOrEmpty(logFilename))
                return;

            var variants = m_Issues.Where(i => i.category == IssueCategory.ShaderVariant).ToArray();
            var result = ShadersModule.ParsePlayerLog(logFilename, variants, new ProgressBar());
            switch (result)
            {
                case ParseLogResult.Success:
                    EditorUtility.DisplayDialog(k_PlayerLogParsingDialogTitle, k_PlayerLogProcessed, k_Ok);
                    MarkDirty();
                    break;
                case ParseLogResult.NoCompiledVariants:
                    if (GraphicsSettingsProxy.logShaderCompilationSupported)
                    {
                        if (GraphicsSettingsProxy.logWhenShaderIsCompiled)
                        {
                            EditorUtility.DisplayDialog(k_PlayerLogParsingDialogTitle, k_NoCompiledVariantWarning, k_Ok);
                        }
                        else
                        {
                            GraphicsSettingsProxy.logWhenShaderIsCompiled = EditorUtility.DisplayDialog(k_PlayerLogParsingDialogTitle, k_NoCompiledVariantWarningLogDisabled, k_Yes, k_No);
                        }
                    }
                    break;
                case ParseLogResult.ReadError:
                    EditorUtility.DisplayDialog(k_PlayerLogParsingDialogTitle, k_PlayerLogReadError, k_Ok);
                    break;
            }
        }

        public override void DrawFilters()
        {
            if (m_Desc.category == IssueCategory.ShaderVariant)
            {
                GUI.enabled = numIssues > 0;

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Extra :", GUILayout.Width(80));
                EditorGUI.BeginChangeCheck();
                m_ShowCompiledVariants = EditorGUILayout.ToggleLeft("Compiled Variants", m_ShowCompiledVariants, GUILayout.Width(180));
                m_ShowUncompiledVariants = EditorGUILayout.ToggleLeft("Uncompiled Variants", m_ShowUncompiledVariants, GUILayout.Width(180));
                if (EditorGUI.EndChangeCheck())
                {
                    MarkDirty();
                }
                EditorGUILayout.EndHorizontal();

                GUI.enabled = true;
            }
        }

        public override void DrawRightPanels(ProjectIssue[] selectedIssues)
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(300));

            for (int i = 0; i < m_PropertyFoldouts.Length; i++)
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);
                m_PropertyFoldouts[i].enabled = Utility.BoldFoldout(m_PropertyFoldouts[i].enabled, m_PropertyFoldouts[i].content);
                if (m_PropertyFoldouts[i].enabled)
                {
                    const int maxHeight = 120;
                    m_PropertyFoldouts[i].scroll = GUILayout.BeginScrollView(m_PropertyFoldouts[i].scroll, GUIStyle.none, GUI.skin.verticalScrollbar, GUILayout.MaxHeight(maxHeight));

                    if (selectedIssues.Length == 0)
                        GUILayout.TextArea("<No selection>", SharedStyles.TextArea, GUILayout.ExpandHeight(true));
                    else
                    {
                        // check if they are all the same
                        var props = selectedIssues.Select(issue =>
                            issue.GetCustomProperty(m_PropertyFoldouts[i].id)).Distinct().ToArray();
                        if (props.Length > 1)
                            GUILayout.TextArea("<Multiple values>", SharedStyles.TextArea, GUILayout.ExpandHeight(true));
                        else // if (props.Length == 1)
                        {
                            var text = Formatting.ReplaceStringSeparators(props[0], "\n");
                            GUILayout.TextArea(text, SharedStyles.TextArea, GUILayout.ExpandHeight(true));
                        }
                    }

                    GUILayout.EndScrollView();
                }
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndVertical();
        }

        protected override void DrawInfo()
        {
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(k_BuildInstructions, SharedStyles.TextArea);
            EditorGUILayout.HelpBox(k_ClearInstructions, MessageType.Warning);

            if (numIssues > 0 && m_Desc.category == IssueCategory.ShaderVariant)
            {
                EditorGUILayout.LabelField(k_PlayerLogInstructions, SharedStyles.TextArea);
                if (!GraphicsSettingsProxy.logShaderCompilationSupported)
                    EditorGUILayout.HelpBox(k_PlayerLogParsingUnsupported, MessageType.Warning);

                if (GraphicsSettingsProxy.logShaderCompilationSupported)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(k_LogShaderCompilation, GUILayout.Width(270));
                    GraphicsSettingsProxy.logWhenShaderIsCompiled = EditorGUILayout.Toggle(GraphicsSettingsProxy.logWhenShaderIsCompiled);
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(k_ExportAsVariantCollection, GUILayout.Width(270));
                m_ExportAsVariantCollection = EditorGUILayout.Toggle(m_ExportAsVariantCollection);
                EditorGUILayout.EndHorizontal();

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

        protected override void Export(Func<ProjectIssue, bool> predicate = null)
        {
            if (!m_ExportAsVariantCollection)
            {
                base.Export(predicate);
                return;
            }

            var path = EditorUtility.SaveFilePanelInProject("Save to SVC file", "NewShaderVariants.shadervariants",
                "shadervariants", "Save SVC");

            var svcName = Path.GetFileNameWithoutExtension(path);
            if (path.Length != 0)
            {
                var variants = m_Issues.Where(issue => predicate == null || predicate(issue));

                ShadersModule.ExportSVC(svcName, path, variants.ToArray());

                EditorUtility.RevealInFinder(path);
            }
        }

        public override bool Match(ProjectIssue issue)
        {
            if (!base.Match(issue))
                return false;
            if (m_Desc.category == IssueCategory.ComputeShaderVariant)
                return true;

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
