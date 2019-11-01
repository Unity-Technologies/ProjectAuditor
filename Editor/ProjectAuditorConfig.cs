using UnityEngine;

namespace Unity.ProjectAuditor.Editor
{
    public class ProjectAuditorConfig : ScriptableObject
    {
        public bool enablePackages = false;
        public bool enableAnalyzeOnBuild = false;
        public bool enableFailBuildOnIssues = false;
    }
}