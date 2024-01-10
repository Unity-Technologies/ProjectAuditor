using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Interfaces;
using Unity.ProjectAuditor.Editor.UI.Framework;
using Unity.ProjectAuditor.Editor.Modules;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Unity.ProjectAuditor.Editor.UI
{
    class BuildDataShaderVariantsView : AnalysisView
    {
        const string k_BulletPointUnicode = " \u2022";

        static readonly string k_Description = $@"This view shows the built Shader Variants.";

        static readonly string k_PlayerLogInstructions = $@"To find out which of these variants are compiled at runtime:
{k_BulletPointUnicode} Enable <b>Settings > Graphics > Shader Preloading > Log Shader Compilation</b> or click the checkbox below.
{k_BulletPointUnicode} Run the build on the target platform. Make sure to go through all scenes.
{k_BulletPointUnicode} Drag & Drop the Player.log file on this window";

        const string k_PlayerLogParsingDialogTitle = "Shader Variants";

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

        internal static readonly Dictionary<string, string> k_StageNameMap = new Dictionary<string, string>()
        {
            { "all", "vertex" },       // GLES* / OpenGLCore
            { "pixel", "fragment" }    // Metal
        };

        public override void Create(ViewDescriptor descriptor, IssueLayout layout, SeverityRules rules, ViewStates viewStates, IIssueFilter filter)
        {
            var propertyFoldouts = new List<PropertyFoldout>();

            propertyFoldouts.Add(new PropertyFoldout
            {
                id = descriptor.Category == IssueCategory.BuildDataShaderVariant ? (int)BuildDataShaderVariantProperty.Keywords : (int)ComputeShaderVariantProperty.Keywords,
                enabled = true,
                content = new GUIContent("Keywords")
            });

            m_PropertyFoldouts = propertyFoldouts.ToArray();

            base.Create(descriptor, layout, rules, viewStates, filter);
        }

        void ParsePlayerLog(string logFilename)
        {
            if (string.IsNullOrEmpty(logFilename))
                return;

            var variants = m_Issues.Where(i => i.Category == IssueCategory.BuildDataShaderVariant).ToArray();
            var result = ParsePlayerLog(logFilename, variants, new ProgressBar());
            switch (result)
            {
                case ParseLogResult.Success:
                    EditorUtility.DisplayDialog(k_PlayerLogParsingDialogTitle, k_PlayerLogProcessed, k_Ok);
                    MarkDirty();
                    break;
                case ParseLogResult.NoCompiledVariants:
                    if (GraphicsSettings.logWhenShaderIsCompiled)
                    {
                        EditorUtility.DisplayDialog(k_PlayerLogParsingDialogTitle, k_NoCompiledVariantWarning, k_Ok);
                    }
                    else
                    {
                        GraphicsSettings.logWhenShaderIsCompiled = EditorUtility.DisplayDialog(k_PlayerLogParsingDialogTitle, k_NoCompiledVariantWarningLogDisabled, k_Yes, k_No);
                    }
                    break;
                case ParseLogResult.ReadError:
                    EditorUtility.DisplayDialog(k_PlayerLogParsingDialogTitle, k_PlayerLogReadError, k_Ok);
                    break;
            }
        }

        public static ParseLogResult ParsePlayerLog(string logFile, ProjectIssue[] builtVariants, IProgress progress = null)
        {
            var compiledVariants = new Dictionary<string, List<CompiledVariantData>>();
            var lines = ShadersModule.GetCompiledShaderLines(logFile);
            if (lines == null)
                return ParseLogResult.ReadError;

            foreach (var line in lines)
            {
                var parts = line.Split(new[] {", pass: ", ", stage: ", ", keywords "}, StringSplitOptions.None);
                if (parts.Length != 4)
                {
                    Debug.LogError("Malformed shader compilation log info: " + line);
                    continue;
                }

                var shaderName = parts[0];
                var pass = parts[1];
                var stage = parts[2];
                var keywordsString = parts[3];
                var keywords = SplitKeywords(keywordsString, " ");

                // fix-up stage to be consistent with built variants stage
                if (k_StageNameMap.ContainsKey(stage))
                    stage = k_StageNameMap[stage];

                if (!compiledVariants.ContainsKey(shaderName))
                {
                    compiledVariants.Add(shaderName, new List<CompiledVariantData>());
                }
                compiledVariants[shaderName].Add(new CompiledVariantData
                {
                    Pass = pass,
                    Stage = stage,
                    Keywords = keywords
                });
            }

            if (!compiledVariants.Any())
                return ParseLogResult.NoCompiledVariants;

            builtVariants = builtVariants.OrderBy(v => v.Description).ToArray();
            var shader = (Shader)null;
            foreach (var builtVariant in builtVariants)
            {
                if (shader == null || !shader.name.Equals(builtVariant.Description))
                {
                    shader = Shader.Find(builtVariant.Description);
                }

                if (shader == null)
                {
                    builtVariant.SetCustomProperty(BuildDataShaderVariantProperty.Compiled, "?");
                    continue;
                }

                var shaderName = shader.name;
                var stage = builtVariant.GetCustomProperty(BuildDataShaderVariantProperty.Stage);
                var passName = builtVariant.GetCustomProperty(BuildDataShaderVariantProperty.PassName);
                var keywordsString = builtVariant.GetCustomProperty(BuildDataShaderVariantProperty.Keywords);
                var keywords = SplitKeywords(keywordsString);
                var isVariantCompiled = false;

                if (compiledVariants.ContainsKey(shaderName))
                {
                    // note that we are not checking pass name since there is an inconsistency regarding "unnamed" passes between build vs compiled
                    var matchingVariants = compiledVariants[shaderName].Where(cv => ShadersModule.ShaderVariantsMatch(cv, stage, passName, keywords)).ToArray();
                    isVariantCompiled = matchingVariants.Length > 0;
                }

                builtVariant.SetCustomProperty(BuildDataShaderVariantProperty.Compiled, isVariantCompiled);
            }

            return ParseLogResult.Success;
        }

        static string[] SplitKeywords(string keywordsString, string separator = null)
        {
            if (keywordsString.Equals(ShadersModule.k_NoKeywords))
                return new string[] {};
            return Formatting.SplitStrings(keywordsString, separator);
        }

        public override void DrawFilters()
        {
            if (m_Desc.Category == IssueCategory.BuildDataShaderVariant)
            {
                GUI.enabled = NumIssues > 0;

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

        public override void DrawDetails(ProjectIssue[] selectedIssues)
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
                        GUILayout.TextArea("<No selection>", SharedStyles.TextAreaWithDynamicSize, GUILayout.ExpandHeight(true));
                    else
                    {
                        // check if they are all the same
                        var props = selectedIssues.Select(issue =>
                            issue.GetCustomProperty(m_PropertyFoldouts[i].id)).Distinct().ToArray();
                        if (props.Length > 1)
                            GUILayout.TextArea("<Multiple values>", SharedStyles.TextAreaWithDynamicSize, GUILayout.ExpandHeight(true));
                        else // if (props.Length == 1)
                        {
                            var text = Formatting.ReplaceStringSeparators(props[0], "\n");
                            GUILayout.TextArea(text, SharedStyles.TextAreaWithDynamicSize, GUILayout.ExpandHeight(true));
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

            EditorGUILayout.LabelField(k_Description, SharedStyles.TextArea);
            bool isVisualShaderView = m_Desc.Category == IssueCategory.BuildDataShaderVariant;

            if (isVisualShaderView)
                EditorGUILayout.LabelField(k_PlayerLogInstructions, SharedStyles.TextArea);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(k_LogShaderCompilation, GUILayout.Width(290));
            GraphicsSettings.logWhenShaderIsCompiled = EditorGUILayout.Toggle(GraphicsSettings.logWhenShaderIsCompiled);
            EditorGUILayout.EndHorizontal();

            if (NumIssues > 0 && isVisualShaderView)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(k_ExportAsVariantCollection, GUILayout.Width(290));
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

                ShadersModule.ExportBuildDataVariantsToSvc(svcName, path, variants.ToArray());

                EditorUtility.RevealInFinder(path);
            }
        }

        public override bool Match(ProjectIssue issue)
        {
            if (!base.Match(issue))
                return false;

            if (m_Desc.Category == IssueCategory.ComputeShaderVariant)
                return true;

            var compiled = issue.GetCustomPropertyBool(ShaderVariantProperty.Compiled);
            if (compiled && m_ShowCompiledVariants)
                return true;
            if (!compiled && m_ShowUncompiledVariants)
                return true;
            return false;
        }

        public BuildDataShaderVariantsView(ViewManager viewManager) : base(viewManager)
        {
        }
    }
}
