using System;

namespace Unity.ProjectAuditor.Editor
{
    [Serializable]
    public class Rule
    {
        public enum Action
        {
            Default,      // default to TBD
            Error,        // fails on build
            Warning,      // logs a warning
            Info,         // logs an info message
            None,         // suppressed, ignored by UI and build
            Hidden        // not visible to user
        }

        public int id;
        public string filter;
        public Action action;
    }
}