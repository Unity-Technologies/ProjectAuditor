using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Unity.ProjectAuditor.Editor.AssemblyUtils;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor
{
    [Serializable]
    internal class ProjectAuditorParams
    {
        /// <summary>
        /// Categories to include in the audit. If null, all categories will be included.
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
        /// The ProjectAuditorRules object which defines which issues should be ignored or given increased severity
        /// </summary>
        public ProjectAuditorRules Rules;

        /// <summary>
        /// The ProjectAuditorDiagnosticParams object which defines the customizable thresholds for reporting certain diagnostics.
        /// </summary>
        public ProjectAuditorDiagnosticParams DiagnosticParams;

        [JsonIgnore]
        [NonSerialized]
        internal ProjectReport ExistingReport;

        /// <summary>
        /// ProjectAuditorParams constructor
        /// </summary>
        /// <param name="copyParamsFromGlobal"> If true, the global ProjectSettings will register DiagnosticParams defaults, save any changes and copy the data into this object. This is usually the desired behaviour, but is not allowed during serialization. </param>
        public ProjectAuditorParams(bool copyParamsFromGlobal = true)
        {
            if (copyParamsFromGlobal)
            {
                // Check for any new defaults (newly-installed package, new user modules, or an updated version of the package since last analysis)
                ProjectAuditorSettings.instance.DiagnosticParams.RegisterParameters();
                ProjectAuditorSettings.instance.Save();

                Rules = new ProjectAuditorRules(ProjectAuditorSettings.instance.Rules);
                DiagnosticParams = new ProjectAuditorDiagnosticParams(ProjectAuditorSettings.instance.DiagnosticParams);
            }

            Platform = BuildTarget.NoTarget;
            CodeOptimization = CodeOptimization.Release;
            CompilationMode = CompilationMode.Player;
        }

        // Copy constructor
        public ProjectAuditorParams(ProjectAuditorParams original)
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
        }
    }
}
