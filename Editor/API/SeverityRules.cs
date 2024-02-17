using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor
{
    // Rules to specify the Severity of individual Issues.
    // Project Auditor interacts with this to Ignore/Display issues, and it's exposed in the Settings window, but we don't need it in the API.
    // Users can simply construct a List<Rule> and pass it to AnalysisParams.WithAdditionalDiagnosticRules()
    [Serializable]
    internal sealed class SeverityRules
    {
        public SeverityRules()
        {
        }

        // Copy constructor
        public SeverityRules(SeverityRules copyFrom)
        {
            foreach (var rule in copyFrom.m_Rules)
            {
                m_Rules.Add(new Rule
                {
                    Severity = rule.Severity,
                    Filter = rule.Filter,
                    Id = rule.Id
                });
            }
        }

        [NonReorderable]
        [JsonProperty("rules")][SerializeField]
        List<Rule> m_Rules = new List<Rule>();

        internal int NumRules => m_Rules.Count;

        internal void AddRule(Rule ruleToAdd)
        {
            if (string.IsNullOrEmpty(ruleToAdd.Filter))
            {
                ruleToAdd.Filter = string.Empty; // make sure it's empty, as opposed to null

                var rules = m_Rules.Where(r => r.Id.Equals(ruleToAdd.Id)).ToArray();
                foreach (var ruleToDelete in rules)
                    m_Rules.Remove(ruleToDelete);
            }

            m_Rules.Add(ruleToAdd);
        }

        internal Rule GetRule(DescriptorId id, string filter = "")
        {
            // do not use Linq to avoid managed allocations
            foreach (var r in m_Rules)
            {
                if (r.Id.Equals(id) && r.Filter.Equals(filter))
                    return r;
            }
            return null;
        }

        internal void ClearRules(DescriptorId id, string filter = "")
        {
            var rules = m_Rules.Where(r => r.Id.Equals(id) && r.Filter.Equals(filter)).ToArray();

            foreach (var rule in rules)
                m_Rules.Remove(rule);
        }

        internal void ClearRules(ReportItem issue)
        {
            var id = issue.Id;
            ClearRules(id, issue.GetContext());
        }

        internal Severity GetAction(DescriptorId id, string filter = "")
        {
            // is there a rule that matches the Filter?
            var projectRule = GetRule(id, filter);
            if (projectRule != null)
                return projectRule.Severity;

            // is there a rule that matches descriptor?
            projectRule = GetRule(id);
            if (projectRule != null)
                return projectRule.Severity;

            return Severity.Default;
        }

        internal void SetRule(ReportItem issue, Severity ruleSeverity)
        {
            var id = issue.Id;

            // FIXME: GetContext will return empty string on code issues after domain reload
            var context = issue.GetContext();
            var rule = GetRule(id, context);

            if (rule == null)
                AddRule(new Rule
                {
                    Id = id,
                    Filter = context,
                    Severity = ruleSeverity
                });
            else
                rule.Severity = ruleSeverity;
        }

        // Only used for testing
        internal void ClearAllRules()
        {
            m_Rules.Clear();
        }
    }
}
