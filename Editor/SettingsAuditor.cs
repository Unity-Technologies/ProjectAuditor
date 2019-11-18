using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.Macros;

namespace Unity.ProjectAuditor.Editor
{
    public class SettingsAuditor : IAuditor
    {
        private System.Reflection.Assembly[] m_Assemblies;
        private List<ProblemDescriptor> m_ProblemDescriptors;
        private AnalyzerHelpers m_Helpers;

        public SettingsAuditor()
        {
            m_Assemblies = AppDomain.CurrentDomain.GetAssemblies();
            m_Helpers = new AnalyzerHelpers();
        }

        public string GetUIName()
        {
            return "Settings";
        }
        
        public void LoadDatabase(string path)
        {
             m_ProblemDescriptors = ProblemDescriptorHelper.LoadProblemDescriptors( path, "ProjectSettings");
        }

        public void Audit(ProjectReport projectReport, ProjectAuditorConfig config)
        {
            var progressBar =
                new ProgressBarDisplay("Analyzing Scripts", "Analyzing project settings", m_ProblemDescriptors.Count);

            foreach (var p in m_ProblemDescriptors)
            {
                progressBar.AdvanceProgressBar();
                SearchAndEval(p, projectReport, config);
            }
            progressBar.ClearProgressBar();
        }

        private void AddIssue(ProblemDescriptor descriptor, ProjectReport projectReport, ProjectAuditorConfig config)
        {
            if (!config.IsRuleAction(descriptor, Rule.Action.None))
            {
                projectReport.AddIssue(new ProjectIssue
                {
                    description = descriptor.description,
                    category = IssueCategory.ProjectSettings,
                    descriptor = descriptor
                });
            }
        }
        
        void SearchAndEval(ProblemDescriptor p, ProjectReport projectReport, ProjectAuditorConfig config)
        {
            if (string.IsNullOrEmpty(p.customevaluator))
            {
                // do we actually need to look in all assemblies? Maybe we can find a way to only evaluate on the right assembly
                foreach (var assembly in m_Assemblies)
                {
                    try
                    {
                        var value = MethodEvaluator.Eval(assembly.Location,
                            p.type, "get_" + p.method, new System.Type[0]{}, new object[0]{});

                        if (value.ToString() == p.value)
                        {
                            AddIssue(p, projectReport, config);
                        
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
                Type helperType = m_Helpers.GetType();
                MethodInfo theMethod = helperType.GetMethod(p.customevaluator);
                bool isIssue = (bool)theMethod.Invoke(m_Helpers, null);

                if (isIssue)
                {
                    AddIssue(p, projectReport, config);
                }
            }
        }
    }
}