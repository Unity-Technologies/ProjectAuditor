using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor
{
    public class ProjectAuditorConfig : ScriptableObject
    {
        public bool enablePackages = false;
        public bool enableAnalyzeOnBuild = false;
        public bool enableFailBuildOnIssues = false;
        public List<Rule> rules = new List<Rule>();

        public bool IsRuleAction(ProblemDescriptor descriptor, Rule.Action action)
        {
            var rule = rules.Where(r => r.id == descriptor.id).FirstOrDefault();
            if (rule == null)
                return false;
            if (rule.action == action)
                return true;
            return false;
        }

    }
}