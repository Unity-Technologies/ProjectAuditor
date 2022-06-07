using System;
using UnityEditor;

namespace Unity.ProjectAuditor.Editor
{
    public class ProjectAuditorParams
    {
        public IssueCategory[] categories;
        public BuildTarget platform;

        public Action<ProjectIssue> onIssueFound;
        public Action<bool> onUpdate;
        public Action onComplete;

        public ProjectAuditorParams()
        {
            platform = EditorUserBuildSettings.activeBuildTarget;
        }

        public ProjectAuditorParams(ProjectAuditorParams original)
        {
            categories = original.categories;
            platform = original.platform;

            onIssueFound = original.onIssueFound;
            onUpdate = original.onUpdate;
            onComplete = original.onComplete;
        }
    }
}
