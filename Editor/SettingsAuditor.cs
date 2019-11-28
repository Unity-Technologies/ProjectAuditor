using System;
using System.Collections.Generic;
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

        public void Audit(ProjectReport projectReport)
        {
            var progressBar =
                new ProgressBarDisplay("Analyzing Scripts", "Analyzing project settings", m_ProblemDescriptors.Count);

            foreach (var descriptor in m_ProblemDescriptors)
            {
                progressBar.AdvanceProgressBar();
                SearchAndEval(descriptor, projectReport);
            }
            progressBar.ClearProgressBar();
        }

        private void AddIssue(ProblemDescriptor descriptor, ProjectReport projectReport)
        {
            projectReport.AddIssue(new ProjectIssue
            {
                category = IssueCategory.ProjectSettings,
                descriptor = descriptor
            });
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
                            AddIssue(descriptor, projectReport);
                        
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
                MethodInfo theMethod = helperType.GetMethod(descriptor.customevaluator);
                bool isIssue = (bool)theMethod.Invoke(m_Helpers, null);

                if (isIssue)
                {
                    AddIssue(descriptor, projectReport);
                }
            }
        }
    }
}