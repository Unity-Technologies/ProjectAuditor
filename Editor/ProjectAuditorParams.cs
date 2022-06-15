using System;
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

        public Action<ProjectIssue> onIssueFound;
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

            onIssueFound = original.onIssueFound;
            onUpdate = original.onUpdate;
            onComplete = original.onComplete;
        }
    }
}
