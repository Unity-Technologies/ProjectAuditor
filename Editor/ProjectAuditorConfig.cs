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

        public void AddRule(Rule rule)
        {
            if (string.IsNullOrEmpty(rule.filter))
                rule.filter = string.Empty;
            rules.Add(rule);
        }

        public Rule GetRule(ProblemDescriptor descriptor, string filter = "")
        {
            var rule = rules.Where(r => r.id == descriptor.id && r.filter.Equals(filter)).FirstOrDefault();
            if (rule != null)
                return rule;
            return rules.Where(r => r.id == descriptor.id && string.IsNullOrEmpty(r.filter)).FirstOrDefault();
        }
        
        public Rule.Action GetAction(ProblemDescriptor descriptor, string filter = "")
        {
            var projectRule = GetRule(descriptor, filter);
            if (projectRule != null)
            {
                return projectRule.action;
            }

            return descriptor.action;
        }
    }
}