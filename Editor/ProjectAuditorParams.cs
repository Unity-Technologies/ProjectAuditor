using System;
using System.Collections.Generic;
using System.IO;
using Unity.ProjectAuditor.Editor.AssemblyUtils;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor
{
    internal class ProjectAuditorParams
    {
        /// <summary>
        /// Categories to include in the audit. If null, all categories will be included.
        /// </summary>
        public IssueCategory[] categories;

        /// <summary>
        /// Analysis platform. The default platform is the currently active build target.
        /// </summary>
        public BuildTarget platform;

        /// <summary>
        /// Assemblies to analyze. If null, all compiled assemblies will be analyzed.
        /// </summary>
        public string[] assemblyNames;

        /// <summary>
        /// Code optimization mode. The default is <see cref="CodeOptimization.Release"/>.
        /// </summary>
        public CodeOptimization codeOptimization;

        /// <summary>
        /// Compilation mode
        /// </summary>
        public CompilationMode compilationMode;

        /// <summary>
        /// Reports a batch of new issues. Note that this be called multiple times per analysis.
        /// </summary>
        public Action<IEnumerable<ProjectIssue>> onIncomingIssues;

        /// <summary>
        /// Notifies that all modules completed their analysis.
        /// </summary>
        public Action<ProjectReport> onCompleted;

        /// <summary>
        /// Notifies that a module completed its analysis.
        /// </summary>
        public Action onModuleCompleted;

        /// <summary>
        /// The ProjectAuditorRules object which defines which issues should be ignored, and the customizable thresholds for reporting certain diagnostics.
        /// </summary>
        public ProjectAuditorRules rules;

        // TODO: Not sure what this is for, so keeping it internal for now. Document it if we need it to be public
        internal ProjectReport existingReport;


        /// <summary>
        /// ProjectAuditorParams constructor
        /// </summary>
        public ProjectAuditorParams()
        {
            platform = EditorUserBuildSettings.activeBuildTarget;
            codeOptimization = CodeOptimization.Release;
            compilationMode = CompilationMode.Player;

            InitRulesAsset(UserPreferences.rulesAssetPath);
        }

        /// <summary>
        /// ProjectAuditorParams constructor
        /// </summary>
        /// <param name="projectAuditorRules"> ProjectAuditorRules object</param>
        public ProjectAuditorParams(ProjectAuditorRules projectAuditorRules)
        {
            rules = projectAuditorRules;
        }

        /// <summary>
        /// ProjectAuditorParams constructor
        /// </summary>
        /// <param name="assetPath"> Path to the ProjectAuditorRules asset</param>
        public ProjectAuditorParams(string assetPath)
        {
            InitRulesAsset(assetPath);
        }

        public ProjectAuditorParams(ProjectAuditorParams original)
        {
            categories = original.categories;
            platform = original.platform;
            assemblyNames = original.assemblyNames;
            codeOptimization = original.codeOptimization;
            compilationMode = original.compilationMode;

            onIncomingIssues = original.onIncomingIssues;
            onCompleted = original.onCompleted;
            onModuleCompleted = original.onModuleCompleted;

            existingReport = original.existingReport;

            rules = original.rules;
        }

        void InitRulesAsset(string assetPath)
        {
            rules = AssetDatabase.LoadAssetAtPath<ProjectAuditorRules>(assetPath);
            if (rules == null)
            {
                var path = Path.GetDirectoryName(assetPath);
                if (!File.Exists(path))
                    Directory.CreateDirectory(path);

                rules = ScriptableObject.CreateInstance<ProjectAuditorRules>();
                rules.Initialize();
                AssetDatabase.CreateAsset(rules, assetPath);

                Debug.LogFormat("Project Auditor Rules: {0} has been created.", assetPath);
            }

            rules.SetAnalysisPlatform(platform);
        }
    }
}
