using System;
using System.Collections.Generic;
using System.Linq;
using Unity.ProjectAuditor.Editor.Interfaces;
using Unity.ProjectAuditor.Editor.Utils; // Required for TypeCache in Unity 2018
using UnityEditor;

namespace Unity.ProjectAuditor.Editor.Core
{
    internal abstract class ProjectAuditorModuleWithAnalyzers<T> : ProjectAuditorModule where T : IModuleAnalyzer
    {
        protected List<T> m_Analyzers;

        protected T[] GetPlatformAnalyzers(BuildTarget platform)
        {
            return m_Analyzers.Where(a => CoreUtils.SupportsPlatform(a.GetType(), platform)).ToArray();
        }

        public override void Initialize(ProjectAuditorRules rules)
        {
            base.Initialize(rules);

            m_Analyzers = new List<T>();

            foreach (var type in TypeCache.GetTypesDerivedFrom(typeof(T)))
            {
                if (type.IsAbstract)
                    continue;
                var moduleAnalyzer = (IModuleAnalyzer)Activator.CreateInstance(type);
                moduleAnalyzer.Initialize(this);
                m_Analyzers.Add((T)moduleAnalyzer);
            }
        }
    }
}
