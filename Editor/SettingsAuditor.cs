using System;
using System.Reflection;
using UnityEditor.Macros;

namespace Unity.ProjectAuditor.Editor
{
    public class SettingsAuditor : IAuditor
    {
        private DefinitionDatabase m_Database;
        private AnalyzerHelpers m_Helpers;

        public SettingsAuditor()
        {
            m_Helpers = new AnalyzerHelpers();
        }

        public string GetUIName()
        {
            return "Settings";
        }
        
        public void LoadDatabase(string path)
        {
            m_Database = new DefinitionDatabase( path, "ProjectSettings");
        }

        public void Audit(ProjectReport projectReport)
        {
            var problemDefinitions = m_Database.m_Definitions;
            var progressBar =
                new ProgressBarDisplay("Analyzing Scripts", "Analyzing project settings", problemDefinitions.Count);

            // do we actually need to look in all assemblies?
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var p in problemDefinitions)
            {
                progressBar.AdvanceProgressBar();
                SearchAndEval(p, assemblies, projectReport);
            }
            progressBar.ClearProgressBar();
        }
     
        void SearchAndEval(ProblemDefinition p, System.Reflection.Assembly[] assemblies, ProjectReport projectReport)
        {
            if (string.IsNullOrEmpty(p.customevaluator))
            {
                // try all assemblies. Need to find a way to only evaluate on the right assembly
                foreach (var assembly in assemblies)
                {
                    try
                    {
                        var value = MethodEvaluator.Eval(assembly.Location,
                            p.type, "get_" + p.method, new System.Type[0]{}, new object[0]{});

                        if (value.ToString() == p.value)
                        {
                            projectReport.AddIssue(new ProjectIssue
                            {
                                description = p.description,
                                category = IssueCategory.ProjectSettings,
                                def = p
                            }, IssueCategory.ProjectSettings);
                        
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
                    projectReport.AddIssue(new ProjectIssue
                    {
                        description = p.description,
                        category = IssueCategory.ProjectSettings,
                        def = p
                    }, IssueCategory.ProjectSettings);
                }
            }
        }
    }
}