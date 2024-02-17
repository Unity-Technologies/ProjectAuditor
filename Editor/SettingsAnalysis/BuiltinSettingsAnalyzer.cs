using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.ProjectAuditor.Editor.Core;
using UnityEditor.Macros;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.SettingsAnalysis
{
    class BuiltinSettingsAnalyzer : SettingsModuleAnalyzer
    {
        readonly List<Assembly> m_Assemblies = new List<Assembly>();
        readonly List<KeyValuePair<string, string>> m_ProjectSettingsMapping =
            new List<KeyValuePair<string, string>>();
        List<Descriptor> m_Descriptors;

        public override void Initialize(Action<Descriptor> registerDescriptor)
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
            m_ProjectSettingsMapping.Add(
                new KeyValuePair<string, string>("UnityEngine.Physics2D", "Project/Physics 2D"));
            m_ProjectSettingsMapping.Add(new KeyValuePair<string, string>("UnityEngine.Physics", "Project/Physics"));
            m_ProjectSettingsMapping.Add(new KeyValuePair<string, string>("UnityEngine.Time", "Project/Time"));
            m_ProjectSettingsMapping.Add(new KeyValuePair<string, string>("UnityEngine.QualitySettings",
                "Project/Quality"));
            m_ProjectSettingsMapping.Add(new KeyValuePair<string, string>("UnityEngine.AudioModule",
                "Project/Audio"));

            m_Descriptors = DescriptorLoader.LoadFromJson(ProjectAuditor.s_DataPath, "ProjectSettings");
            foreach (var descriptor in m_Descriptors)
            {
                registerDescriptor(descriptor);
            }
        }

        public override IEnumerable<ReportItem> Analyze(SettingsAnalysisContext context)
        {
            if (m_Descriptors == null)
                throw new Exception("Descriptors Database not initialized.");

            foreach (var descriptor in m_Descriptors.Where(d => d.IsApplicable(context.Params)))
            {
                var issue = Evaluate(context, descriptor);
                if (issue != null)
                    yield return issue;
            }
        }

        ReportItem Evaluate(AnalysisContext context, Descriptor descriptor)
        {
            // evaluate a Unity API static method or property
            var assembly = m_Assemblies.First(a => a.GetType(descriptor.Type) != null);
            var type = assembly.GetType(descriptor.Type);

            var methodName = descriptor.Method;
            var property = type.GetProperty(descriptor.Method);
            if (property != null)
                methodName = "get_" + descriptor.Method;

            var paramTypes = new Type[] {};
            var args = new object[] {};

            try
            {
                var value = MethodEvaluator.Eval(assembly.Location,
                    descriptor.Type, methodName, paramTypes, args);

                if (value.ToString() == descriptor.Value)
                    return NewIssue(context, descriptor, descriptor.Title);
            }
            catch (ArgumentException e)
            {
                Debug.LogWarning($"Could not evaluate {descriptor.Type}.{methodName}. Exception: {e.Message}");
            }

            return null;
        }

        ReportItem NewIssue(AnalysisContext context, Descriptor descriptor, string description)
        {
            var projectWindowPath = string.Empty;
            var mappings = m_ProjectSettingsMapping.Where(p => descriptor.Type.StartsWith(p.Key)).ToArray();
            if (mappings.Any())
                projectWindowPath = mappings.First().Value;
            return context.CreateIssue
                (
                    IssueCategory.ProjectSetting,
                    descriptor.Id,
                    description
                ).WithLocation(projectWindowPath);
        }
    }
}
