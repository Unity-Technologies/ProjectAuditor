using System;
using UnityEditor;

namespace Unity.ProjectAuditor.Editor
{
    public class ProjectAuditorParams
    {
        public IssueCategory[] categories;
        public BuildTarget target;

        public Action<ProjectIssue> onIssueFound;
        public Action<bool> onUpdate;
        public Action onComplete;

        public ProjectAuditorParams()
        {
            target = EditorUserBuildSettings.activeBuildTarget;
        }

        public ProjectAuditorParams(ProjectAuditorParams original)
        {
            categories = original.categories;
            target = original.target;

            onIssueFound = original.onIssueFound;
            onUpdate = original.onUpdate;
            onComplete = original.onComplete;
        }
    }
}
