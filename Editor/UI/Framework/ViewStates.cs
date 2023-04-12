using System;

namespace Unity.ProjectAuditor.Editor.UI.Framework
{
    /// <summary>
    /// Global UI states. Note that these preferences will not persist between sessions.
    /// </summary>
    [Serializable]
    internal class ViewStates
    {
        internal const int k_MinFontSize = 12;
        internal const int k_MaxFontSize = 22;

        // foldout preferences
        internal bool info = true;
        internal bool filters = true;
        internal bool actions = true;
        internal bool dependencies = true;

        // diagnostic preferences
        internal bool onlyCriticalIssues;
        internal bool mutedIssues;

        internal int fontSize = k_MinFontSize;
    }
}
