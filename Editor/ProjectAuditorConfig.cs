using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor
{
    public class ProjectAuditorConfig : ScriptableObject
    {
        public bool displayMutedIssues;
        public bool enableAnalyzeOnBuild;
        public bool enableFailBuildOnIssues;
        private readonly List<Rule> m_Rules = new List<Rule>();

        public int NumRules => m_Rules.Count;

        public void AddRule(Rule ruleToAdd)
        {
            if (string.IsNullOrEmpty(ruleToAdd.filter))
            {
                ruleToAdd.filter = string.Empty; // make sure it's empty, as opposed to null

                var rules = m_Rules.Where(r => r.id == ruleToAdd.id).ToArray();
                foreach (var ruleToDelete in rules) m_Rules.Remove(ruleToDelete);
            }

            m_Rules.Add(ruleToAdd);
        }

        public Rule GetRule(ProblemDescriptor descriptor, string filter = "")
        {
            return m_Rules.FirstOrDefault(r => r.id == descriptor.id && r.filter.Equals(filter));
        }

        public void ClearAllRules()
        {
            m_Rules.Clear();
        }

        public void ClearRules(ProblemDescriptor descriptor, string filter = "")
        {
            var rules = m_Rules.Where(r => r.id == descriptor.id && r.filter.Equals(filter)).ToArray();

            foreach (var rule in rules) m_Rules.Remove(rule);
        }

        public Rule.Action GetAction(ProblemDescriptor descriptor, string filter = "")
        {
            // is there a rule that matches the filter?
            var projectRule = GetRule(descriptor, filter);
            if (projectRule != null) return projectRule.action;

            // is there a rule that matches descriptor?
            projectRule = GetRule(descriptor);
            if (projectRule != null) return projectRule.action;

            // return the default descriptor action
            return descriptor.action;
        }
    }
}