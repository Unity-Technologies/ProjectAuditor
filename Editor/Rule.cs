using System;

namespace Unity.ProjectAuditor.Editor
{
    [Serializable]
    public class Rule
    {
        public enum Action
        {
            Error,
            Warning,
            Info,
            None,
            Hidden,
            Default
        }

        public int id;
        public Action action;
    }
}