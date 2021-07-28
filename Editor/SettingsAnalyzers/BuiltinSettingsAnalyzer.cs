using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor.Macros;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.SettingsAnalyzers
{
    class BuiltinSettingsAnalyzer : ISettingsAnalyzer
    {
        readonly List<Assembly> m_Assemblies = new List<Assembly>();
        readonly List<KeyValuePair<string, string>> m_ProjectSettingsMapping =
            new List<KeyValuePair<string, string>>();
        List<ProblemDescriptor> m_ProblemDescriptors;

        public void Initialize(ProjectAuditorModule module)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            m_Assemblies.Add(assemblies.First(a => a.Location.Contains("UnityEngine.dll")));
            m_Assemblies.Add(assemblies.First(a => a.Location.Contains("UnityEditor.dll")));

            // UnityEditor
            m_ProjectSettingsMapping.Add(new KeyValuePair<string, string>("UnityEditor.PlayerSettings",
                "Project/Player"));
            m_ProjectSettingsMapping.Add(
                new KeyValuePair<string, string>("UnityEditor.Rendering.EditorGraphicsSettings", "Project/Graphics"));

            // UnityEngine
            m_ProjectSettingsMapping.Add(new KeyValuePair<string, string>("UnityEngine.Physics", "Project/Physics"));
            m_ProjectSettingsMapping.Add(
                new KeyValuePair<string, string>("UnityEngine.Physics2D", "Project/Physics 2D"));
            m_ProjectSettingsMapping.Add(new KeyValuePair<string, string>("UnityEngine.Time", "Project/Time"));
            m_ProjectSettingsMapping.Add(new KeyValuePair<string, string>("UnityEngine.QualitySettings",
                "Project/Quality"));

            m_ProblemDescriptors = ProblemDescriptorLoader.LoadFromJson(ProjectAuditor.DataPath, "ProjectSettings");
            foreach (var descriptor in m_ProblemDescriptors)
            {
                module.RegisterDescriptor(descriptor);
            }
        }

        public IEnumerable<ProjectIssue> Analyze()
        {
            if (m_ProblemDescriptors == null)
                throw new Exception("Descriptors Database not initialized.");

            foreach (var descriptor in m_ProblemDescriptors)
            {
                var issue = SearchAndEval(descriptor);
                if (issue != null)
                    yield return issue;
            }
        }

        ProjectIssue SearchAndEval(ProblemDescriptor descriptor)
        {
            if (string.IsNullOrEmpty(descriptor.customevaluator))
            {
                var paramTypes = new Type[] {};
                var args = new object[] {};
                var found = false;
                // do we actually need to look in all assemblies? Maybe we can find a way to only evaluate on the right assembly
                foreach (var assembly in m_Assemblies)
                    try
                    {
                        var value = MethodEvaluator.Eval(assembly.Location,
                            descriptor.type, "get_" + descriptor.method, paramTypes, args);

                        if (value.ToString() == descriptor.value)
                        {
                            return NewIssue(descriptor, descriptor.description);
                        }

                        // Eval did not throw exception so we can stop iterating assemblies
                        found = true;
                        break;
                    }
                    catch (Exception)
                    {
                        // this is safe to ignore
                    }

                if (!found)
                    Debug.Log(descriptor.method + " not found in any assembly");
            }
            else
            {
                var evalType = typeof(Evaluators);
                var method = evalType.GetMethod(descriptor.customevaluator);
                if ((bool)method.Invoke(null, null))
                    return NewIssue(descriptor, descriptor.description);
            }

            return null;
        }

        ProjectIssue NewIssue(ProblemDescriptor descriptor, string description)
        {
            var projectWindowPath = string.Empty;
            var mappings = m_ProjectSettingsMapping.Where(p => descriptor.type.StartsWith(p.Key));
            if (mappings.Any())
                projectWindowPath = mappings.First().Value;
            return new ProjectIssue
            (
                descriptor,
                description,
                IssueCategory.ProjectSetting,
                new Location(projectWindowPath)
            );
        }
    }
}
