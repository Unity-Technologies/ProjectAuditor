using System;
using System.Collections.Generic;
using UnityEditor;

namespace Unity.ProjectAuditor.Editor
{
    public class ProjectAuditorParams
    {
        public IssueCategory[] categories;
        public BuildTarget platform;

        public Action<IReadOnlyCollection<ProjectIssue>> onModuleCompleted; //should be called on main thread

        public ProjectAuditorParams()
        {
            platform = EditorUserBuildSettings.activeBuildTarget;
        }

        public ProjectAuditorParams(ProjectAuditorParams original)
        {
            categories = original.categories;
            platform = original.platform;

            onModuleCompleted = original.onModuleCompleted;
        }
    }
}
