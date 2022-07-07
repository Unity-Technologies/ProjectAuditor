using System;

namespace Unity.ProjectAuditor.Editor.UI.Framework
{
    /// <summary>
    /// Global UI states. Note that these preferences will not persist between sessions.
    /// </summary>
    [Serializable]
    public class GlobalStates
    {
        public const int k_MinFontSize = 12;
        public const int k_MaxFontSize = 22;

        // foldout preferences
        public bool info = true;
        public bool filters = true;
        public bool actions = true;
        public bool dependencies = true;
        public bool details = true;
        public bool recommendation = true;

        // diagnostic preferences
        public bool onlyCriticalIssues;
        public bool mutedIssues;

        public int fontSize = k_MinFontSize;
    }
}
