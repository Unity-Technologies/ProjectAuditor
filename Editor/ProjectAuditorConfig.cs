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
        public bool displayMutedIssues = false;
        private List<Rule> m_Rules = new List<Rule>();

        public void AddRule(Rule rule)
        {
            if (string.IsNullOrEmpty(rule.filter))
                rule.filter = string.Empty;
            m_Rules.Add(rule);
        }

        public Rule GetRule(ProblemDescriptor descriptor, string filter = "")
        {
            var rule = m_Rules.FirstOrDefault(r => r.id == descriptor.id && r.filter.Equals(filter));
            if (rule != null)
                return rule;
            return m_Rules.FirstOrDefault(r => r.id == descriptor.id && string.IsNullOrEmpty(r.filter));
        }

        public void ClearRules(ProblemDescriptor descriptor, string filter = "")
        {
            var rules = m_Rules.Where(r => r.id == descriptor.id && r.filter.Equals(filter));
            
            foreach (var rule in rules)
            {
                m_Rules.Remove(rule);
            }   
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