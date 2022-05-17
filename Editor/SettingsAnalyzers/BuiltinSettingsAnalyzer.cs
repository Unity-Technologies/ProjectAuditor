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
                var issue = Evaluate(descriptor);
                if (issue != null)
                    yield return issue;
            }
        }

        ProjectIssue Evaluate(ProblemDescriptor descriptor)
        {
            if (string.IsNullOrEmpty(descriptor.customevaluator))
            {
                var assembly = m_Assemblies.First(a => a.GetType(descriptor.type) != null);
                var type = assembly.GetType(descriptor.type);

                var methodName = descriptor.method;
                var property = type.GetProperty(descriptor.method);
                if (property != null)
                    methodName = "get_" + descriptor.method;

                var paramTypes = new Type[] {};
                var args = new object[] {};

                var value = MethodEvaluator.Eval(assembly.Location,
                    descriptor.type, methodName, paramTypes, args);

                if (value.ToString() == descriptor.value)
                    return NewIssue(descriptor, descriptor.description);
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
            var mappings = m_ProjectSettingsMapping.Where(p => descriptor.type.StartsWith(p.Key)).ToArray();
            if (mappings.Any())
                projectWindowPath = mappings.First().Value;
            return new ProjectIssue
            (
                descriptor,
                IssueCategory.ProjectSetting,
                description
            )
            {
                location = new Location(projectWindowPath)
            };
        }
    }
}
