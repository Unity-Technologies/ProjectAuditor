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
    public class ProjectAuditorRules : ScriptableObject
    {
        //////////////////////////////////////////////////////////////////////
        // Ignore/Filter rules

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
            var id = issue.id;
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
            var id = issue.id;

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

        //////////////////////////////////////////////////////////////////////
        // Per-platform Diagnostic Params

        [Serializable]
        internal class DiagnosticParams : ISerializationCallbackReceiver
        {
            public BuildTarget Platform;

            // A string-keyed Dictionary is not a particularly efficient data structure. However:
            // - We need strings for serialization, so we can't just hash strings to make keys then throw the string away
            // - We want DiagnosticParams to be arbitrarily-definable by any future module without modifying core code, which rules out an enum
            // It can stay. For now.
            Dictionary<string, int> m_Params;

            // Can't use KeyValuePair<string, int> because Unity won't serialize generic types. So we'll make a concrete type.
            [Serializable]
            internal struct ParamKeyValue
            {
                public string Key;
                public int Value;

                public ParamKeyValue(string key, int value)
                {
                    Key = key;
                    Value = value;
                }
            }

            [SerializeField] List<ParamKeyValue> m_SerializedParams;

            public DiagnosticParams()
            {
                m_Params = new Dictionary<string, int>();
            }

            public DiagnosticParams(BuildTarget platform) : this()
            {
                Platform = platform;
            }

            public bool TryGetParameter(string paramName, out int paramValue)
            {
                return m_Params.TryGetValue(paramName, out paramValue);
            }

            public void SetParameter(string paramName, int paramValue)
            {
                m_Params[paramName] = paramValue;
            }

            public void OnBeforeSerialize()
            {
                m_SerializedParams = new List<ParamKeyValue>();
                foreach (var key in m_Params.Keys)
                {
                    m_SerializedParams.Add(new ParamKeyValue(key, m_Params[key]));
                }
            }

            public void OnAfterDeserialize()
            {
                if (m_SerializedParams != null)
                {
                    m_Params.Clear();
                    foreach (var kvp in m_SerializedParams)
                    {
                        m_Params[kvp.Key] = kvp.Value;
                    }

                    m_SerializedParams = null;
                }
            }
        }

        [SerializeField] internal List<DiagnosticParams> m_ParamsStack;
        [SerializeField] public int CurrentParamsIndex;

        public void Initialize()
        {
            if (m_ParamsStack == null)
            {
                m_ParamsStack = new List<DiagnosticParams>();
            }

            if (m_ParamsStack.Count == 0)
            {
                // We treat BuildTarget.NoTarget as the default value fallback if there isn't a platform-specific override
                m_ParamsStack.Add(new DiagnosticParams(BuildTarget.NoTarget));
            }

            EditorUtility.SetDirty(this);
        }

        public void SetAnalysisPlatform(BuildTarget platform)
        {
            for (int i = 0; i < m_ParamsStack.Count; ++i)
            {
                if (m_ParamsStack[i].Platform == platform)
                {
                    CurrentParamsIndex = i;
                    return;
                }
            }

            // We didn't find this platform in the platform stack yet, so let's create it.
            m_ParamsStack.Add(new DiagnosticParams(platform));
            CurrentParamsIndex = m_ParamsStack.Count - 1;

            EditorUtility.SetDirty(this);
        }

        public int GetParameter(string paramName, int defaultValue)
        {
            if (m_ParamsStack.Count == 0)
            {
                Debug.LogError("Uninitialized ProjectAuditorRules. Find out how this one was created and why it wasn't initialized.");
            }

            int paramValue;

            // Try the params for the current analysis platform
            if (CurrentParamsIndex < m_ParamsStack.Count)
            {
                if (m_ParamsStack[CurrentParamsIndex].TryGetParameter(paramName, out paramValue))
                    return paramValue;
            }

            // Try the default
            if (m_ParamsStack[0].TryGetParameter(paramName, out paramValue))
                return paramValue;

            // We didn't find the parameter in the rules. That's okay, just means we need to register it and set the default value
            m_ParamsStack[0].SetParameter(paramName, defaultValue);
            EditorUtility.SetDirty(this);
            return defaultValue;
        }

        public void SetParameter(BuildTarget platform, string paramName, int value)
        {
            if (m_ParamsStack.Count == 0)
            {
                Debug.LogError("Uninitialized ProjectAuditorRules. Find out how this one was created and why it wasn't initialized.");
            }

            foreach (var platformParams in m_ParamsStack)
            {
                if (platformParams.Platform == platform)
                {
                    platformParams.SetParameter(paramName, value);
                    EditorUtility.SetDirty(this);
                    return;
                }
            }

            var newParams = new DiagnosticParams(platform);
            newParams.SetParameter(paramName, value);
            m_ParamsStack.Add(newParams);
            EditorUtility.SetDirty(this);
        }

        public void Save()
        {
            for (int i = 0; i < m_ParamsStack.Count; ++i)
            {
                m_ParamsStack[i].OnBeforeSerialize();
            }
#if UNITY_2020_3_OR_NEWER
            AssetDatabase.SaveAssetIfDirty(this);
#else
            AssetDatabase.SaveAssets();
#endif
        }
    }
}
