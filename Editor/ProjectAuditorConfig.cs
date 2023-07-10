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
        /// <summary>
        /// If enabled, ProjectAuditor will run every time the project is built.
        /// </summary>
        internal bool AnalyzeOnBuild;

        /// <summary>
        /// If enabled, ProjectAuditor will try to partially analyze the project in the background.
        /// </summary>
        internal bool AnalyzeInBackground = true;

        /// <summary>
        /// Compilation mode
        /// </summary>
        internal CompilationMode CompilationMode;

        /// <summary>
        /// If enabled, ProjectAuditor will use Roslyn Analyzer DLLs that are present in the project
        /// </summary>
        internal bool UseRoslynAnalyzers;

        /// <summary>
        /// If enabled, any issue reported by ProjectAuditor will cause the build to fail.
        /// </summary>
        internal bool FailBuildOnIssues;

        [SerializeField]
        List<Rule> m_Rules = new List<Rule>();

        internal int NumRules => m_Rules.Count;

        internal ProjectAuditorSettings[] Settings;

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
    }
}
