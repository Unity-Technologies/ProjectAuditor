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
        // Public members

        /// <summary>
        /// If enabled, ProjectAuditor will run every time the project is built.
        /// </summary>
        public bool AnalyzeOnBuild;

        /// <summary>
        /// If enabled, ProjectAuditor will try to partially analyze the project in the background.
        /// </summary>
        public bool AnalyzeInBackground = true;

        /// <summary>
        /// Compilation mode
        /// </summary>
        public CompilationMode CompilationMode;

        /// <summary>
        /// If enabled, ProjectAuditor will use Roslyn Analyzer DLLs that are present in the project
        /// </summary>
        public bool UseRoslynAnalyzers;

        /// <summary>
        /// If enabled, any issue reported by ProjectAuditor will cause the build to fail.
        /// </summary>
        public bool FailBuildOnIssues;

        // Internal members

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

        internal Rule GetRule(Descriptor descriptor, string filter = "")
        {
            return GetRule(descriptor.id, filter);
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

        internal void ClearRules(Descriptor descriptor, string filter = "")
        {
            ClearRules(descriptor.id, filter);
        }

        internal void ClearRules(ProjectIssue issue)
        {
            var descriptor = issue.descriptor;
            ClearRules(descriptor, issue.GetContext());
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

        internal Severity GetAction(Descriptor descriptor, string filter = "")
        {
            return GetAction(descriptor.id, filter);
        }

        internal void SetRule(ProjectIssue issue, Severity ruleSeverity)
        {
            var descriptor = issue.descriptor;

            // FIXME: GetContext will return empty string on code issues after domain reload
            var context = issue.GetContext();
            var rule = GetRule(descriptor, context);

            if (rule == null)
                AddRule(new Rule
                {
                    id = descriptor.id,
                    filter = context,
                    severity = ruleSeverity
                });
            else
                rule.severity = ruleSeverity;

            EditorUtility.SetDirty(this);
        }
    }
}
