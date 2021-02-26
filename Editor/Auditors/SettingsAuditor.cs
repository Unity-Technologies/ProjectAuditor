using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.ProjectAuditor.Editor.SettingsAnalyzers;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor.Macros;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Auditors
{
    class SettingsAuditor : IAuditor
    {
        static readonly IssueLayout k_IssueLayout = new IssueLayout
        {
            category = IssueCategory.ProjectSettings,
            properties = new[]
            {
                new IssueProperty { type = PropertyType.Description, name = "Issue", longName = "Issue description"},
                new IssueProperty { type = PropertyType.Area, name = "Area", longName = "The area the issue might have an impact on"}
            }
        };

        readonly List<Assembly> m_Assemblies = new List<Assembly>();
        readonly Evaluators m_Helpers = new Evaluators();

        readonly List<KeyValuePair<string, string>> m_ProjectSettingsMapping =
            new List<KeyValuePair<string, string>>();

        readonly Dictionary<int, ISettingsAnalyzer> m_SettingsAnalyzers =
            new Dictionary<int, ISettingsAnalyzer>();
        List<ProblemDescriptor> m_ProblemDescriptors;

        public void Initialize(ProjectAuditorConfig config)
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
        }

        public IEnumerable<ProblemDescriptor> GetDescriptors()
        {
            return m_ProblemDescriptors;
        }

        public IEnumerable<IssueLayout> GetLayouts()
        {
            yield return k_IssueLayout;
        }

        public void Reload(string path)
        {
            m_ProblemDescriptors = ProblemDescriptorLoader.LoadFromJson(path, "ProjectSettings");

            foreach (var type in AssemblyHelper.GetAllTypesInheritedFromInterface<ISettingsAnalyzer>())
                AddAnalyzer(Activator.CreateInstance(type) as ISettingsAnalyzer);
        }

        public void RegisterDescriptor(ProblemDescriptor descriptor)
        {
            m_ProblemDescriptors.Add(descriptor);
        }

        public void Audit(Action<ProjectIssue> onIssueFound, Action onComplete, IProgressBar progressBar = null)
        {
            if (m_ProblemDescriptors == null)
                throw new Exception("Issue Database not initialized.");

            if (progressBar != null)
                progressBar.Initialize("Analyzing Settings", "Analyzing project settings", m_ProblemDescriptors.Count);

            foreach (var descriptor in m_ProblemDescriptors)
            {
                if (progressBar != null)
                    progressBar.AdvanceProgressBar();

                if (m_SettingsAnalyzers.ContainsKey(descriptor.id))
                {
                    var analyzer = m_SettingsAnalyzers[descriptor.id];
                    var projectIssue = analyzer.Analyze();
                    if (projectIssue != null) onIssueFound(projectIssue);
                }
                else
                {
                    SearchAndEval(descriptor, onIssueFound);
                }
            }

            if (progressBar != null)
                progressBar.ClearProgressBar();

            onComplete();
        }

        void AddAnalyzer(ISettingsAnalyzer analyzer)
        {
            analyzer.Initialize(this);
            m_SettingsAnalyzers.Add(analyzer.GetDescriptorId(), analyzer);
        }

        void AddIssue(ProblemDescriptor descriptor, string description, Action<ProjectIssue> onIssueFound)
        {
            var projectWindowPath = "";
            var mappings = m_ProjectSettingsMapping.Where(p => p.Key.Contains(descriptor.type));
            if (mappings.Count() > 0)
                projectWindowPath = mappings.First().Value;
            onIssueFound(new ProjectIssue
                (
                    descriptor,
                    description,
                    IssueCategory.ProjectSettings,
                    new Location(projectWindowPath)
                )
            );
        }

        void SearchAndEval(ProblemDescriptor descriptor, Action<ProjectIssue> onIssueFound)
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
                            AddIssue(descriptor, descriptor.description, onIssueFound);
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
                var helperType = m_Helpers.GetType();
                var theMethod = helperType.GetMethod(descriptor.customevaluator);
                var isIssue = (bool)theMethod.Invoke(m_Helpers, null);
                if (isIssue) AddIssue(descriptor, descriptor.description, onIssueFound);
            }
        }
    }
}
