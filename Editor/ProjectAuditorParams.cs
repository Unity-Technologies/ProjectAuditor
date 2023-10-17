using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Unity.ProjectAuditor.Editor.AssemblyUtils;
using UnityEditor;

namespace Unity.ProjectAuditor.Editor
{
    internal class ProjectAuditorParams
    {
        /// <summary>
        /// Categories to include in the audit. If null, all categories will be included.
        /// </summary>
        public IssueCategory[] Categories;

        /// <summary>
        /// Analysis platform. The default platform is the currently active build target.
        /// </summary>
        [JsonIgnore]
        public BuildTarget Platform;

        [JsonProperty("Platform")]
        public string PlatformString
        {
            get => Platform.ToString();
            set => Platform = (BuildTarget)Enum.Parse(typeof(BuildTarget), value);
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

        [JsonIgnore]
        [NonSerialized]
        public ProjectReport ExistingReport;

        public ProjectAuditorDiagnosticParams DiagnosticParams;

        public ProjectAuditorParams()
        {
            Platform = EditorUserBuildSettings.activeBuildTarget;
            CodeOptimization = CodeOptimization.Release;
            CompilationMode = CompilationMode.Player;
        }

        public ProjectAuditorParams(BuildTarget platform)
        {
            Platform = platform;
            CodeOptimization = CodeOptimization.Release;
            CompilationMode = CompilationMode.Player;
        }

        public ProjectAuditorParams(ProjectAuditorParams original)
        {
            Categories = original.Categories;
            Platform = original.Platform;
            AssemblyNames = original.AssemblyNames;
            CodeOptimization = original.CodeOptimization;
            CompilationMode = original.CompilationMode;

            OnIncomingIssues = original.OnIncomingIssues;
            OnCompleted = original.OnCompleted;
            OnModuleCompleted = original.OnModuleCompleted;

            ExistingReport = original.ExistingReport;

            DiagnosticParams = original.DiagnosticParams;
        }
    }
}
