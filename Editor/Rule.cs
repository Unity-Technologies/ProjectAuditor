using System;

namespace Unity.ProjectAuditor.Editor
{
    [Serializable]
    public class Rule : IEquatable<Rule>
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
        
        public override bool Equals(object obj)
        {
            return this.Equals(obj as Rule);
        }
        
        public bool Equals(Rule other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            if (other.GetType() != GetType()) return false;
            return id == other.id && filter == other.filter && action == other.action;
        }
        
        public static bool operator == (Rule a, Rule b)
        {
            if (Object.ReferenceEquals(a, null))
            {
                if (Object.ReferenceEquals(a, null))
                    return true;
                return false;
            }
            return a.Equals(b);
        }
        
        public static bool operator != (Rule a, Rule b)
        {
            return !(a == b);
        }
        
        public override int GetHashCode()
        {
            unchecked {
                int hash = 17;
                hash = hash * 23 + id.GetHashCode();
                hash = hash * 23 + filter.GetHashCode();
                hash = hash * 23 + action.GetHashCode();
                return hash;
            }
        }
    }
}