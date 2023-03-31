using System;
using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.AssemblyUtils;
using UnityEditor;

namespace Unity.ProjectAuditor.Editor
{
    internal class ProjectAuditorParams
    {
        /// <summary>
        /// Categories to include in the audit. If null, all categories will be included.
        /// </summary>
        internal IssueCategory[] categories;

        /// <summary>
        /// Analysis platform. The default platform is the currently active build target.
        /// </summary>
        internal BuildTarget platform;

        /// <summary>
        /// Assemblies to analyze. If null, all compiled assemblies will be analyzed.
        /// </summary>
        internal string[] assemblyNames;

        /// <summary>
        /// Code optimization mode. The default is <see cref="CodeOptimization.Release"/>.
        /// </summary>
        internal CodeOptimization codeOptimization;

        /// <summary>
        /// Reports a batch of new issues. Note that this be called multiple times per analysis.
        /// </summary>
        internal Action<IEnumerable<ProjectIssue>> onIncomingIssues;

        /// <summary>
        /// Notifies that all modules completed their analysis.
        /// </summary>
        internal Action<ProjectReport> onCompleted;

        /// <summary>
        /// Notifies that a module completed its analysis.
        /// </summary>
        internal Action onModuleCompleted;

        internal ProjectReport existingReport;

        internal ProjectAuditorSettings settings;

        internal ProjectAuditorParams()
        {
            platform = EditorUserBuildSettings.activeBuildTarget;
            codeOptimization = CodeOptimization.Release;
        }

        internal ProjectAuditorParams(ProjectAuditorParams original)
        {
            categories = original.categories;
            platform = original.platform;
            assemblyNames = original.assemblyNames;
            codeOptimization = original.codeOptimization;

            onIncomingIssues = original.onIncomingIssues;
            onCompleted = original.onCompleted;
            onModuleCompleted = original.onModuleCompleted;

            existingReport = original.existingReport;

            settings = original.settings;
        }
    }
}
