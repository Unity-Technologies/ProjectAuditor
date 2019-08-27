using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using UnityEditor.Macros;

namespace Unity.ProjectAuditor.Editor
{
    public class ProjectAuditor
    {
        private UnityEditor.Compilation.Assembly[] m_PlayerAssemblies;
        
        private DefinitionDatabase m_ApiCalls;
        private DefinitionDatabase m_ProjectSettings;

        private AnalyzerHelpers m_Helpers;

        private string[] m_WhitelistedPackages;

        public ProjectAuditor()
        {
            m_Helpers = new AnalyzerHelpers();          
            m_PlayerAssemblies = CompilationPipeline.GetAssemblies(AssembliesType.Player);
            
            LoadDatabase();
        }

        void SetupPackageWhitelist()
        {
            var fullPath = Path.GetFullPath($"Packages/com.unity.project-auditor/Data/PackageWhitelist.txt");
            var whitelist = File.ReadAllText(fullPath);
            m_WhitelistedPackages = whitelist.Replace("\r\n", "\n").Split('\n');
        }
        
        public ProjectReport Audit()
        {
            var projectReport = new ProjectReport();

            AnalyzeApiCalls(m_ApiCalls.m_Definitions, projectReport);
            AnalyzeProjectSettings(m_ProjectSettings.m_Definitions, projectReport);

            return projectReport;
        }

        public void LoadDatabase()
        {
            m_ApiCalls = new DefinitionDatabase("ApiDatabase");
            m_ProjectSettings = new DefinitionDatabase("ProjectSettings");

            SetupPackageWhitelist();           
        }

        public void AnalyzeApiCalls(List<ProblemDefinition> problemDefinitions, ProjectReport projectReport)
        {
            Debug.Log("Analyzing Scripts...");

            // Analyse all Player assemblies, including Package assemblies.
            foreach (var playerAssembly in m_PlayerAssemblies)
            {
                string assemblyPath = playerAssembly.outputPath;
                if (!File.Exists(assemblyPath))
                {
                    Debug.LogError($"{assemblyPath} not found.");
                    return;
                }
                
                using (var a = AssemblyDefinition.ReadAssembly(assemblyPath, new ReaderParameters() { ReadSymbols = true} ))
                {
                    foreach (var m in a.MainModule.Types.SelectMany(t => t.Methods))
                    {
                        if (!m.HasBody)
                            continue;
                
                        foreach (var inst in m.Body.Instructions.Where(i => (i.OpCode == OpCodes.Call || i.OpCode == OpCodes.Callvirt)))
                        {
                            var calledMethod = ((MethodReference) inst.Operand);
                            
                            // HACK: need to figure out a way to know whether a method is actually a property
                            var p = problemDefinitions.SingleOrDefault(c => c.type == calledMethod.DeclaringType.FullName &&
                                                                            (c.method == calledMethod.Name || ("get_" + c.method) == calledMethod.Name));

                            if (p == null)
                            {
                                // Are we trying to warn about a whole namespace?
                                p = problemDefinitions.SingleOrDefault(c =>
                                    c.type == calledMethod.DeclaringType.Namespace && c.method == "*");
                            }
                            //if (p.type != null && m.HasCustomDebugInformations)
                            if (p != null && m.DebugInformation.HasSequencePoints)
                            {
                                var msg = string.Empty;
                                SequencePoint s = null;
                                for (var i = inst; i != null; i = i.Previous)
                                {
                                    s = m.DebugInformation.GetSequencePoint(i);
                                    if (s != null)
                                    {
                                        msg = i == inst ? " exactly" : "nearby";
                                        break;
                                    }
                                }
                
                                if (s != null)
                                {
                                    // Ignore whitelisted packages
                                    // (SteveM - I'd put this code further up in one of the outer loops but I don't
                                    // know if it's possible to get the URL further up to compare with the whitelist)
                                    bool isPackageWhitelisted = false;
                                    foreach (string package in m_WhitelistedPackages)
                                    {
                                        if (s.Document.Url.Contains(package))
                                        {
                                            isPackageWhitelisted = true;
                                            break;
                                        }                       
                                    }

                                    if (!isPackageWhitelisted)
                                    {
                                        projectReport.m_ApiCallsIssues.Add(new ProjectIssue
                                        {
                                            category = "API Call",
                                            def = p,
                                            url = s.Document.Url,
                                            line = s.StartLine,
                                            column = s.StartColumn
                                        });
                                    }
                                }
                            }
                        }
                    }
                }                
            }
            

        }

        void SearchAndEval(ProblemDefinition p, System.Reflection.Assembly[] assemblies, ProjectReport projectReport)
        {
            if (string.IsNullOrEmpty(p.customevaluator))
            {
                foreach (var assembly in assemblies)
                {
                    try
                    {
                        var value = MethodEvaluator.Eval(assembly.Location,
                            p.type, "get_" + p.method, new System.Type[0]{}, new object[0]{});

                        if (value.ToString() == p.value)
                        {
                            projectReport.m_ProjectSettingsIssues.Add(new ProjectIssue
                            {
                                category = "ProjectSettings",
                                def = p
                            });
                        
                            // stop iterating assemblies
                            break;
                        }
                    }
                    catch (Exception e)
                    {
                        // TODO
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
                    projectReport.m_ProjectSettingsIssues.Add(new ProjectIssue
                    {
                        category = "ProjectSettings",
                        def = p
                    });
                }
            }
        }
               
        public void AnalyzeProjectSettings(List<ProblemDefinition> problemDefinitions, ProjectReport projectReport)
        {
            Debug.Log("Analyzing Project Settings...");
//            string [] assemblyNames = new string[]{"UnityEditor.dll", "UnityEngine.dll", "UnityEditor.WebGL.Extensions.dll"}; 
//            var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(x => assemblyNames.Contains(x.ManifestModule.Name));
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var p in problemDefinitions)
            {
                SearchAndEval(p, assemblies, projectReport);
            }
        }
    }
}