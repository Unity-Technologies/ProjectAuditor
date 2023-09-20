using System;
using System.Collections.Generic;
using System.Linq;
using Unity.ProjectAuditor.Editor.AssemblyUtils;
using Unity.ProjectAuditor.Editor.Diagnostic;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor
{
    /// <summary>
    /// Project-specific settings
    /// </summary>
    public class ProjectAuditorConfig : ScriptableObject
    {
        [SerializeField]
        List<Rule> m_Rules = new List<Rule>();

        internal int NumRules => m_Rules.Count;

        internal void AddRule(Rule ruleToAdd)
        {
            if (string.IsNullOrEmpty(ruleToAdd.filter))
            {
                ruleToAdd.filter = string.Empty; // make sure it's empty, as opposed to null

                var rules = m_Rules.Where(r => r.id == ruleToAdd.id).ToArray();
                foreach (var ruleToDelete in rules)
                    m_Rules.Remove(ruleToDelete);
            }

            m_Rules.Add(ruleToAdd);

            EditorUtility.SetDirty(this);
        }

        internal Rule GetRule(string id, string filter = "")
        {
            // do not use Linq to avoid managed allocations
            foreach (var r in m_Rules)
            {
                if (r.id == id && r.filter.Equals(filter))
                    return r;
            }
            return null;
        }

        internal void ClearAllRules()
        {
            m_Rules.Clear();

            EditorUtility.SetDirty(this);
        }

        internal void ClearRules(string id, string filter = "")
        {
            var rules = m_Rules.Where(r => r.id == id && r.filter.Equals(filter)).ToArray();

            foreach (var rule in rules)
                m_Rules.Remove(rule);

            EditorUtility.SetDirty(this);
        }

        internal void ClearRules(ProjectIssue issue)
        {
            var id = issue.Id;
            ClearRules(id, issue.GetContext());
        }

        internal Severity GetAction(string id, string filter = "")
        {
            // is there a rule that matches the filter?
            var projectRule = GetRule(id, filter);
            if (projectRule != null)
                return projectRule.severity;

            // is there a rule that matches descriptor?
            projectRule = GetRule(id);
            if (projectRule != null)
                return projectRule.severity;

            return Severity.Default;
        }

        internal void SetRule(ProjectIssue issue, Severity ruleSeverity)
        {
            var id = issue.Id;

            // FIXME: GetContext will return empty string on code issues after domain reload
            var context = issue.GetContext();
            var rule = GetRule(id, context);

            if (rule == null)
                AddRule(new Rule
                {
                    id = id,
                    filter = context,
                    severity = ruleSeverity
                });
            else
                rule.severity = ruleSeverity;

            EditorUtility.SetDirty(this);
        }
    }
}
