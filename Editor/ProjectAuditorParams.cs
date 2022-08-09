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

        public Action<IEnumerable<ProjectIssue>> onIncomingIssues;
        public Action<ProjectReport> onUpdate;
        public Action onComplete;

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
            onUpdate = original.onUpdate;
            onComplete = original.onComplete;
        }
    }
}
