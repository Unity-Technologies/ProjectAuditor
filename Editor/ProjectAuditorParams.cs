using System;
using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.AssemblyUtils;
using UnityEditor;

namespace Unity.ProjectAuditor.Editor
{
    public class ProjectAuditorParams
    {
        public IssueCategory[] categories;
        public BuildTarget platform;
        public string[] assemblyNames;
        public CodeOptimization codeOptimization;

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

        public ProjectAuditorParams()
        {
            platform = EditorUserBuildSettings.activeBuildTarget;
            codeOptimization = CodeOptimization.Release;
        }

        public ProjectAuditorParams(ProjectAuditorParams original)
        {
            categories = original.categories;
            platform = original.platform;
            assemblyNames = original.assemblyNames;
            codeOptimization = original.codeOptimization;

            onIncomingIssues = original.onIncomingIssues;
            onCompleted = original.onCompleted;
            onModuleCompleted = original.onModuleCompleted;
        }
    }
}
