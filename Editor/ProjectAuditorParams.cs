using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Unity.ProjectAuditor.Editor.AssemblyUtils;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor
{
    internal class SerializedAnalysisParams
    {
        /// <summary>
        /// Categories to include in the audit. If null, all categories will be included.
        /// </summary>
        public IssueCategory[] Categories;

        BuildTarget m_Platform;
        string m_PlatformString;

        /// <summary>
        /// Analysis platform. The default platform is the currently active build target.
        /// </summary>
        [JsonIgnore]
        public BuildTarget Platform
        {
            get => m_Platform;
            set
            {
                m_Platform = value;
                m_PlatformString = m_Platform.ToString();
            }
        }

        [JsonProperty("Platform")]
        public string PlatformString
        {
            get => m_PlatformString;
            set
            {
                m_PlatformString = value;
                m_Platform = (BuildTarget)Enum.Parse(typeof(BuildTarget), m_PlatformString);
            }
        }

        /// <summary>
        /// Assemblies to analyze. If null, all compiled assemblies will be analyzed.
        /// </summary>
        public string[] AssemblyNames;

        /// <summary>
        /// Code optimization mode. The default is <see cref="CodeOptimization.Release"/>.
        /// </summary>
        public CodeOptimization CodeOptimization;

        /// <summary>
        /// Compilation mode
        /// </summary>
        public CompilationMode CompilationMode;

        public SerializedAnalysisParams()
        {
        }

        public SerializedAnalysisParams(SerializedAnalysisParams original)
        {
            Categories = original.Categories;
            Platform = original.Platform;
            AssemblyNames = original.AssemblyNames;
            CodeOptimization = original.CodeOptimization;
            CompilationMode = original.CompilationMode;
        }
    }

    internal class ProjectAuditorParams : SerializedAnalysisParams
    {
        /// <summary>
        /// Reports a batch of new issues. Note that this be called multiple times per analysis.
        /// </summary>
        [JsonIgnore]
        public Action<IEnumerable<ProjectIssue>> OnIncomingIssues;

        /// <summary>
        /// Notifies that all modules completed their analysis.
        /// </summary>
        [JsonIgnore]
        public Action<ProjectReport> OnCompleted;

        /// <summary>
        /// Notifies that a module completed its analysis.
        /// </summary>
        [JsonIgnore]
        public Action OnModuleCompleted;

        /// <summary>
        /// The ProjectAuditorRules object which defines which issues should be ignored, and the customizable thresholds for reporting certain diagnostics.
        /// </summary>
        [JsonIgnore]
        public ProjectAuditorRules Rules;

        [JsonIgnore]
        [NonSerialized]
        internal ProjectReport ExistingReport;

        /// <summary>
        /// ProjectAuditorParams constructor
        /// </summary>
        public ProjectAuditorParams()
        : this(EditorUserBuildSettings.activeBuildTarget, UserPreferences.RulesAssetPath)
        {
        }

        /// <summary>
        /// ProjectAuditorParams constructor
        /// </summary>
        /// <param name="platform"> Target platform for analysis</param>
        /// <param name="assetPath"> Path to the ProjectAuditorRules asset</param>
        public ProjectAuditorParams(BuildTarget platform, string assetPath)
        {
            Platform = platform;
            CodeOptimization = CodeOptimization.Release;
            CompilationMode = CompilationMode.Player;

            InitRulesAsset(assetPath);
        }

        public ProjectAuditorParams(ProjectAuditorParams original)
        : base(original)
        {
            OnIncomingIssues = original.OnIncomingIssues;
            OnCompleted = original.OnCompleted;
            OnModuleCompleted = original.OnModuleCompleted;

            ExistingReport = original.ExistingReport;

            Rules = original.Rules;
        }

        void InitRulesAsset(string assetPath)
        {
            Rules = AssetDatabase.LoadAssetAtPath<ProjectAuditorRules>(assetPath);
            if (Rules == null)
            {
                var path = Path.GetDirectoryName(assetPath);
                if (!File.Exists(path))
                    Directory.CreateDirectory(path);

                Rules = ScriptableObject.CreateInstance<ProjectAuditorRules>();
                Rules.Initialize();
                AssetDatabase.CreateAsset(Rules, assetPath);

                Debug.LogFormat("Project Auditor Rules: {0} has been created.", assetPath);
            }

            Rules.SetAnalysisPlatform(Platform);
        }
    }
}
