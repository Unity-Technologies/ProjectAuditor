using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.ProjectAuditor.Editor.SettingsAnalyzers;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor.Macros;

namespace Unity.ProjectAuditor.Editor.Auditors
{
    public class SettingsAuditor : IAuditor
    {
        private List<ProblemDescriptor> m_ProblemDescriptors;
        
        private readonly System.Reflection.Assembly[] m_Assemblies;
        private readonly Evaluators m_Helpers = new Evaluators();
        private readonly List<KeyValuePair<string, string>> m_ProjectSettingsMapping = new List<KeyValuePair<string, string>>();
        private readonly Dictionary<int, ISettingsAnalyzer> m_SettingsAnalyzers = new Dictionary<int, ISettingsAnalyzer>();
        
        internal SettingsAuditor(ProjectAuditorConfig config)
        {
            m_Assemblies = AppDomain.CurrentDomain.GetAssemblies();
            
            // UnityEditor
            m_ProjectSettingsMapping.Add(new KeyValuePair<string, string>("UnityEditor.PlayerSettings", "Project/Player"));
            m_ProjectSettingsMapping.Add(new KeyValuePair<string, string>("UnityEditor.Rendering.EditorGraphicsSettings", "Project/Graphics"));

            // UnityEngine
            m_ProjectSettingsMapping.Add(new KeyValuePair<string, string>("UnityEngine.Physics", "Project/Physics"));
            m_ProjectSettingsMapping.Add(new KeyValuePair<string, string>("UnityEngine.Physics2D", "Project/Physics 2D"));
            m_ProjectSettingsMapping.Add(new KeyValuePair<string, string>("UnityEngine.Time", "Project/Time"));
            m_ProjectSettingsMapping.Add(new KeyValuePair<string, string>("UnityEngine.QualitySettings", "Project/Quality"));
        }

        public IEnumerable<ProblemDescriptor> GetDescriptors()
        {
            return m_ProblemDescriptors;
        }
        
        public void LoadDatabase(string path)
        {
             m_ProblemDescriptors = ProblemDescriptorHelper.LoadProblemDescriptors( path, "ProjectSettings");
             
             foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
             {
                 foreach (var type in GetAnalyzerTypes(assembly))
                 {
                     AddAnalyzer(Activator.CreateInstance(type, this) as ISettingsAnalyzer);
                 }
             }

        }

        public IEnumerable<Type> GetAnalyzerTypes(System.Reflection.Assembly assembly)
        {
            foreach(var type in assembly.GetTypes())
            {
                if (type.GetCustomAttributes(typeof(SettingsAnalyzers.Attribute), true).Length > 0)
                {
                    yield return type;
                }
            }
        }

        void AddAnalyzer(ISettingsAnalyzer analyzer)
        {
            m_SettingsAnalyzers.Add(analyzer.GetDescriptorId(), analyzer);
        }

        public void RegisterDescriptor(ProblemDescriptor descriptor)
        {
            m_ProblemDescriptors.Add(descriptor);
        }

        public void Audit(ProjectReport projectReport, IProgressBar progressBar = null)
        {
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
                    if (projectIssue != null)
                    {
                        projectReport.AddIssue(projectIssue);
                    }
                }
                else
                {
                    SearchAndEval(descriptor, projectReport);    
                }
            }
            if (progressBar != null)
                progressBar.ClearProgressBar();
        }

        private void AddIssue(ProblemDescriptor descriptor, string description, ProjectReport projectReport)
        {
            string projectWindowPath = "";
            var mappings = m_ProjectSettingsMapping.Where(p => p.Key.Contains(descriptor.type));
            if (mappings.Count() > 0)
                projectWindowPath = mappings.First().Value;
            projectReport.AddIssue(new ProjectIssue
            (
                descriptor,
                description,
                IssueCategory.ProjectSettings,
                new Location {path = projectWindowPath}
            ));
        }
        
        private void SearchAndEval(ProblemDescriptor descriptor, ProjectReport projectReport)
        {
            if (string.IsNullOrEmpty(descriptor.customevaluator))
            {
                // do we actually need to look in all assemblies? Maybe we can find a way to only evaluate on the right assembly
                foreach (var assembly in m_Assemblies)
                {
                    try
                    {
                        var value = MethodEvaluator.Eval(assembly.Location,
                            descriptor.type, "get_" + descriptor.method, new System.Type[0]{}, new object[0]{});

                        if (value.ToString() == descriptor.value)
                        {
                            AddIssue(descriptor, string.Format("{0}: {1}", descriptor.description, value), projectReport);
                        
                            // stop iterating assemblies
                            break;
                        }
                    }
                    catch (Exception)
                    {
                        // this is safe to ignore
                    }
                }
            }
            else
            {
                var helperType = m_Helpers.GetType();
                var theMethod = helperType.GetMethod(descriptor.customevaluator);
                var isIssue = (bool)theMethod.Invoke(m_Helpers, null);

                if (isIssue)
                {
                    AddIssue(descriptor, descriptor.description, projectReport);
                }
            }
        }
    }
}