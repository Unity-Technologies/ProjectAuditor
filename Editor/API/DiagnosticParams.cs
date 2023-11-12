using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Utils; // Required for TypeCache in Unity 2018
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor
{
    // stephenm TODO: Elaborate on this, and document all the public methods.
    /// <summary>
    /// Project-specific settings
    /// </summary>
    [Serializable]
    public sealed class DiagnosticParams : ISerializationCallbackReceiver
    {
        public DiagnosticParams()
        {
            // We treat BuildTarget.NoTarget as the default value fallback if there isn't a platform-specific override
            m_ParamsStack.Add(new PlatformParams(BuildTarget.NoTarget));
        }

        // Copy constructor
        public DiagnosticParams(DiagnosticParams copyFrom)
        {
            foreach (var platformParams in copyFrom.m_ParamsStack)
            {
                m_ParamsStack.Add(new PlatformParams(platformParams));
            }
        }

        internal void RegisterParameters()
        {
            m_ParamsStack[0].Platform = BuildTarget.NoTarget;
            foreach (var type in TypeCache.GetTypesDerivedFrom(typeof(Module)))
            {
                if (type.IsAbstract)
                    continue;
                var instance = Activator.CreateInstance(type) as Module;
                instance.RegisterParameters(this);
            }
        }

        [Serializable]
        internal class PlatformParams
        {
            [JsonIgnore]
            public BuildTarget Platform;

            [JsonProperty("platform")]
            string PlatformString
            {
                get => Platform.ToString();
                set => Platform = (BuildTarget)Enum.Parse(typeof(BuildTarget), value);
            }

            // A string-keyed Dictionary is not a particularly efficient data structure. However:
            // - We need strings for serialization, so we can't just hash strings to make keys then throw the string away
            // - We want DiagnosticParams to be arbitrarily-definable by any future module without modifying core code, which rules out an enum
            // It can stay. For now.
            [JsonProperty("params")]
            Dictionary<string, int> m_Params = new Dictionary<string, int>();

            internal int ParamsCount => (m_Params == null) ? 0 : m_Params.Count;

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

#if UNITY_2020_2_OR_NEWER
            [NonReorderable]
#endif
            [JsonIgnore] [SerializeField]
            List<ParamKeyValue> m_SerializedParams = new List<ParamKeyValue>();

            public PlatformParams()
            {
            }

            public PlatformParams(BuildTarget platform) : this()
            {
                Platform = platform;
            }

            public PlatformParams(PlatformParams copyFrom) : this()
            {
                Platform = copyFrom.Platform;

                foreach (var key in copyFrom.m_Params.Keys)
                {
                    SetParameter(key, copyFrom.m_Params[key]);
                }
            }

            public bool TryGetParameter(string paramName, out int paramValue)
            {
                return m_Params.TryGetValue(paramName, out paramValue);
            }

            public void SetParameter(string paramName, int paramValue)
            {
                m_Params[paramName] = paramValue;
            }

            public void PreSerialize()
            {
                m_SerializedParams.Clear();
                foreach (var key in m_Params.Keys)
                {
                    m_SerializedParams.Add(new ParamKeyValue(key, m_Params[key]));
                }
            }

            public void PostDeserialize()
            {
                m_Params.Clear();
                foreach (var kvp in m_SerializedParams)
                {
                    m_Params[kvp.Key] = kvp.Value;
                }

                m_SerializedParams.Clear();
            }

            // For testing purposes only
            internal IEnumerable<string> GetKeys()
            {
                return m_Params.Keys;
            }
        }

        public void OnBeforeSerialize()
        {
            EnsureDefaults();

            foreach (var platformParams in m_ParamsStack)
            {
                platformParams.PreSerialize();
            }
        }

        public void OnAfterDeserialize()
        {
            foreach (var platformParams in m_ParamsStack)
            {
                platformParams.PostDeserialize();
            }

            EnsureDefaults();
        }

        void EnsureDefaults()
        {
            if (m_ParamsStack == null || m_ParamsStack.Count == 0)
            {
                m_ParamsStack = new List<PlatformParams>();
                m_ParamsStack.Add(new PlatformParams(BuildTarget.NoTarget));
            }

            if (m_ParamsStack[0].ParamsCount == 0)
            {
                RegisterParameters();
            }
        }

#if UNITY_2020_2_OR_NEWER
        [NonReorderable]
#endif
        [JsonProperty("paramsStack")] [SerializeField]
        internal List<PlatformParams> m_ParamsStack = new List<PlatformParams>();

        [JsonProperty] [SerializeField]
        public int CurrentParamsIndex;

        public void SetAnalysisPlatform(BuildTarget platform)
        {
            EnsureDefaults();

            for (int i = 0; i < m_ParamsStack.Count; ++i)
            {
                if (m_ParamsStack[i].Platform == platform)
                {
                    CurrentParamsIndex = i;
                    return;
                }
            }

            // We didn't find this platform in the platform stack yet, so let's create it.
            m_ParamsStack.Add(new PlatformParams(platform));
            CurrentParamsIndex = m_ParamsStack.Count - 1;
        }

        public void RegisterParameter(string paramName, int defaultValue)
        {
            // Does this check mean that parameter default values can't be automatically changed if they change in future versions of the package?
            // Yep. Nothing is perfect. This is better than the risk of over-writing values that users may have tweaked.
            if (!m_ParamsStack[0].TryGetParameter(paramName, out var paramValue))
            {
                // We didn't find the parameter in the defaults. So add it.
                m_ParamsStack[0].SetParameter(paramName, defaultValue);
            }
        }

        public int GetParameter(string paramName)
        {
            int paramValue;

            // Try the params for the current analysis platform
            if (CurrentParamsIndex > 0 && CurrentParamsIndex < m_ParamsStack.Count)
            {
                if (m_ParamsStack[CurrentParamsIndex].TryGetParameter(paramName, out paramValue))
                    return paramValue;
            }

            // Try the default
            if (m_ParamsStack[0].TryGetParameter(paramName, out paramValue))
                return paramValue;

            // We didn't find the parameter in the rules.
            throw new Exception($"Diagnostic parameter '{paramName}' not found. Check that it is properly registered");
        }

        public void SetParameter(BuildTarget platform, string paramName, int value)
        {
            foreach (var platformParams in m_ParamsStack)
            {
                if (platformParams.Platform == platform)
                {
                    platformParams.SetParameter(paramName, value);
                    return;
                }
            }

            var newParams = new PlatformParams(platform);
            newParams.SetParameter(paramName, value);
            m_ParamsStack.Add(newParams);
        }

        // For testing purposes only
        internal void ClearAllParameters()
        {
            m_ParamsStack.Clear();
            m_ParamsStack.Add(new PlatformParams(BuildTarget.NoTarget));
        }

        // For testing purposes only
        internal int CountParameters()
        {
            var foundParams = new HashSet<string>();

            foreach (var platformParams in m_ParamsStack)
            {
                foreach (var key in platformParams.GetKeys())
                {
                    foundParams.Add(key);
                }
            }

            return foundParams.Count;
        }
    }
}
