using System;
using System.Collections.Generic;
using UnityEditor;

namespace Unity.ProjectAuditor.Editor
{
    public class ProjectAuditorParams
    {
        public IssueCategory[] categories;
        public BuildTarget platform;

        public Action<IReadOnlyCollection<ProjectIssue>> onAuditAsyncUpdate;
        public Action<ProjectReport> onAuditAsyncComplete;

        public ProjectAuditorParams()
        {
            platform = EditorUserBuildSettings.activeBuildTarget;
        }

        public ProjectAuditorParams(ProjectAuditorParams original)
        {
            categories = original.categories;
            platform = original.platform;

            onAuditAsyncUpdate = original.onAuditAsyncUpdate;
            onAuditAsyncComplete = original.onAuditAsyncComplete;
        }
    }
}
