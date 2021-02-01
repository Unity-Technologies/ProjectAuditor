using System;

namespace Unity.ProjectAuditor.Editor.UI
{
    [Serializable]
    class Preferences
    {
        public const int k_MinFontSize = 12;
        public const int k_MaxFontSize = 22;

        // foldout preferences
        public bool filters = true;
        public bool actions = true;
        public bool dependencies = true;
        public bool details = true;
        public bool recommendation = true;

        // issues preferences
        public bool onlyCriticalIssues;
        public bool mutedIssues;
        public bool emptyGroups;

        public int fontSize = k_MinFontSize;
    }
}
