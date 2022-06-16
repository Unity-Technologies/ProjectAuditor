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

        public Action<IReadOnlyCollection<ProjectIssue>> onAuditAsyncUpdate;
        public Action<ProjectReport> onAuditAsyncComplete;

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

            onAuditAsyncUpdate = original.onAuditAsyncUpdate;
            onAuditAsyncComplete = original.onAuditAsyncComplete;
        }
    }
}
