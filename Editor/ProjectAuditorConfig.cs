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
            var ruleAction = descriptor.action;
            var projectRule = rules.Where(r => r.id == descriptor.id).FirstOrDefault();
            if (projectRule != null)
            {
                if (projectRule.action != Rule.Action.Default)
                    ruleAction = projectRule.action;
            }
            
            if (action == ruleAction)
                return true;
            return false;
        }

        public Rule.Action GetAction(ProblemDescriptor descriptor)
        {
            var projectRule = rules.Where(r => r.id == descriptor.id).FirstOrDefault();
            if (projectRule != null)
            {
                return projectRule.action;
            }

            return descriptor.action;
        }
    }
}