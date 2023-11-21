using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Unity.ProjectAuditor.Editor.AssemblyUtils;
using Unity.ProjectAuditor.Editor.BuildData;
using UnityEditor;
using UnityEngine;
using SerializedObject = Unity.ProjectAuditor.Editor.BuildData.SerializedObjects.SerializedObject;

namespace Unity.ProjectAuditor.Editor
{
    /// <summary>
    /// Represents an object which can be passed to an instance of <see cref="ProjectAuditor"/> to specify how analysis should be performed and to provide delegates to be called when analysis steps have completed.
    /// AnalysisParams defaults to values which instruct ProjectAuditor to analyse everything in the project for the current build target, but instances can be populated with custom data in an object initializer to provide additional constraints.
    /// </summary>
    [Serializable]
    public class AnalysisParams
    {
        /// <summary>
        /// Issue Categories to include in the audit. If null, the analysis will include all categories except for those relating to assets.
        /// </summary>
        [SerializeField]
        public IssueCategory[] Categories;

        [SerializeField]
        BuildTarget m_Platform;

        [SerializeField]
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
                DiagnosticParams?.SetAnalysisPlatform(Platform);
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
        /// The Compilation mode to use during code analysis. The default is <see cref="CompilationMode.Player"/>.
        /// </summary>
        public CompilationMode CompilationMode;

        /// <summary>
        /// Reports a batch of new issues. Note that this be called multiple times per analysis.
        /// </summary>
        [JsonIgnore]
        public Action<IEnumerable<ProjectIssue>> OnIncomingIssues;

        /// <summary>
        /// Notifies that all Modules completed their analysis.
        /// </summary>
        [JsonIgnore]
        public Action<ProjectReport> OnCompleted;

        /// <summary>
        /// Notifies that a Module completed its analysis.
        /// </summary>
        [JsonIgnore]
        public Action OnModuleCompleted;

        /// <summary>
        /// The DiagnosticParams object which defines the customizable thresholds for reporting certain diagnostics.
        /// By default, this makes a copy of ProjectAuditorSettings.<see cref="ProjectAuditorSettings.DiagnosticParams"/>.
        /// </summary>
        public DiagnosticParams DiagnosticParams;

        // AnalysisParams copy of the global rules. Can be added to with WithAdditionalDiagnosticRules but doesn't need
        // to be exposed to the API.
        [JsonProperty("Rules")]
        internal SeverityRules Rules;

        [JsonIgnore]
        [NonSerialized]
        internal ProjectReport ExistingReport;

        [JsonProperty("Platform")]
        internal string PlatformString
        {
            get => m_PlatformString;
            set
            {
                m_PlatformString = value;
                m_Platform = (BuildTarget)Enum.Parse(typeof(BuildTarget), m_PlatformString);
            }
        }

        public BuildObjects BuildObjects;

        /// <summary>
        /// AnalysisParams constructor.
        /// </summary>
        /// <param name="copyParamsFromGlobal">If true, the global ProjectSettings will register DiagnosticParams defaults, save any changes and copy the data into this object. This is usually the desired behaviour, but is not allowed during serialization. </param>
        public AnalysisParams(bool copyParamsFromGlobal = true)
        {
            if (copyParamsFromGlobal)
            {
                // Check for any new defaults (newly-installed package, new user modules, or an updated version of the package since last analysis)
                ProjectAuditorSettings.instance.DiagnosticParams.RegisterParameters();
                ProjectAuditorSettings.instance.Save();

                Rules = new SeverityRules(ProjectAuditorSettings.instance.Rules);
                DiagnosticParams = new DiagnosticParams(ProjectAuditorSettings.instance.DiagnosticParams);
            }

            Platform = BuildTarget.NoTarget;
            CodeOptimization = CodeOptimization.Release;
            CompilationMode = CompilationMode.Player;
        }

        /// <summary>
        /// Copy constructor.
        /// </summary>
        /// <param name="original">The AnalysisParams object to copy from.</param>
        public AnalysisParams(AnalysisParams original)
        {
            Rules = original.Rules;
            DiagnosticParams = original.DiagnosticParams;

            Categories = original.Categories;
            Platform = original.Platform;
            AssemblyNames = original.AssemblyNames;
            CodeOptimization = original.CodeOptimization;
            CompilationMode = original.CompilationMode;

            OnIncomingIssues = original.OnIncomingIssues;
            OnCompleted = original.OnCompleted;
            OnModuleCompleted = original.OnModuleCompleted;

            ExistingReport = original.ExistingReport;

            BuildObjects = original.BuildObjects;
        }

        /// <summary>
        /// Adds a list of additional Rules which will be applied during analysis.
        /// </summary>
        /// <param name="rules">Additional Rules to impose.</param>
        /// <returns>This AnalysisParams object, after adding the additional Rules.</returns>
        public AnalysisParams WithAdditionalDiagnosticRules(List<Diagnostic.Rule> rules)
        {
            foreach (var rule in rules)
            {
                Rules.AddRule(rule);
            }

            return this;
        }
    }
}
