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

        // TODO: Maybe this should live in some separate class that we hold an instance of, or something. Just throw it in here with the rules for now, refactor later if it needs it.

        [Serializable]
        internal class DiagnosticParams : ISerializationCallbackReceiver
        {
            public string Platform;

            // TODO: string-keyed Dictionary again. Aargh.
            Dictionary<string, int> m_Params = new();

            [Serializable]
            internal struct KeyValuePair
            {
                public string Key;
                public int Value;

                public KeyValuePair(string key, int value)
                {
                    Key = key;
                    Value = value;
                }
            }

            [SerializeField] internal List<KeyValuePair> m_SerializedParams = new();

            public DiagnosticParams(string platform)
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
                m_SerializedParams.Clear();
                foreach (var key in m_Params.Keys)
                {
                    m_SerializedParams.Add(new KeyValuePair(key, m_Params[key]));
                }
            }

            public void OnAfterDeserialize()
            {
                foreach (var kvp in m_SerializedParams)
                {
                    m_Params[kvp.Key] = kvp.Value;
                }
                m_SerializedParams.Clear();
            }
        }

        [SerializeField] internal List<DiagnosticParams> m_ParamsStack = new();
        [SerializeField] internal int m_CurrentParamsIndex; // TODO: Need a way to actually select the index

        public int GetParameter(string paramName)
        {
            if (m_ParamsStack.Count == 0)
            {
                CreateDefaultParams();
            }

            int paramValue;

            // Try the params for the current analysis platform
            if (m_CurrentParamsIndex < m_ParamsStack.Count)
            {
                if (m_ParamsStack[m_CurrentParamsIndex].TryGetParameter(paramName, out paramValue))
                    return paramValue;
            }

            // Try the default
            if (m_ParamsStack[0].TryGetParameter(paramName, out paramValue))
                return paramValue;

            Debug.LogError($"Could not find Diagnostic Parameter '{paramName}' in ProjectAuditorRules");
            return 0;
        }

        void CreateDefaultParams()
        {
            var defaultParams = new DiagnosticParams("Default");

            defaultParams.SetParameter("MeshVerticeCountLimit",5000);
            defaultParams.SetParameter("MeshTriangleCountLimit",5000);
            defaultParams.SetParameter("TextureSizeLimit",2048);
            defaultParams.SetParameter("StreamingAssetsFolderSizeLimit",50);
            defaultParams.SetParameter("TextureStreamingMipmapsSizeLimit",4000);
            defaultParams.SetParameter("SpriteAtlasEmptySpaceLimit",50);
            defaultParams.SetParameter("StreamingClipThresholdBytes",1 * (64000 + (int)(1.6 * 48000 * 2)) + 694);
            defaultParams.SetParameter("LongDecompressedClipThresholdBytes",200 * 1024);
            defaultParams.SetParameter("LongCompressedMobileClipThresholdBytes",200 * 1024);
            defaultParams.SetParameter("LoadInBackGroundClipSizeThresholdBytes",200 * 1024);

            m_ParamsStack.Add(defaultParams);
        }

    }
}
