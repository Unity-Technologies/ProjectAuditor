using System;

namespace Unity.ProjectAuditor.Editor.UI
{
    [Serializable]
    internal class Preferences
    {
        // foldout preferences
        public bool filters = true;
        public bool actions = true;
        public bool dependencies = true;
        public bool details = true;
        public bool recommendation = true;

        // issues preferences
        public bool onlyCriticalIssues = false;
        public bool mutedIssues = false;
        public bool emptyGroups = false;
    }
}
