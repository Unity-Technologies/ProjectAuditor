using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Mono.Cecil;
using Mono.Cecil.Cil;
using UnityEditor;
using UnityEngine;
using UnityEditor.Macros;

namespace Editor
{
    public class ProjectReport
    {
        private DefinitionDatabase m_ApiCalls;
        private DefinitionDatabase m_ProjectSettings;

        public List<ProjectIssue> m_ProjectIssues = new List<ProjectIssue>();

        public ProjectReport()
        {
            m_ApiCalls = new DefinitionDatabase("ApiDatabase");
            m_ProjectSettings = new DefinitionDatabase("ProjectSettings");
        }
        
        public void Create()
        {
            AnalyzeApiCalls(m_ApiCalls.m_Definitions);
            AnalyzeProjectSettings(m_ProjectSettings.m_Definitions);
        }                

        public void AnalyzeApiCalls(List<ProblemDefinition> problemDefinitions)
        {
            string assemblyPath = "Library/ScriptAssemblies/Assembly-CSharp.dll";
            if (!File.Exists(assemblyPath))
            {
                Debug.LogError("Assembly-CSharp.dll not found.");
                return;
            }

            Debug.Log("Analyzing project...");
            using (var a = AssemblyDefinition.ReadAssembly(assemblyPath, new ReaderParameters() { ReadSymbols = true} ))
            {
    //            var callInstructions = a.MainModule.Types.SelectMany(t => t.Methods)
    //                .Where(m => m.HasBody)
    //                .SelectMany(m => m.Body.Instructions)
    //                .Where(i => i.OpCode == Mono.Cecil.Cil.OpCodes.Call);
    //
    //            var myProblems = problemDefinitions
    //                .Where(problem =>
    //                    callInstructions.Any(ci => ((MethodReference) ci.Operand).DeclaringType.Name == problem.type.Name))
    //                .Select(p => new Issue {def = p});
    //
    //            issues.AddRange(myProblems);

                foreach (var m in a.MainModule.Types.SelectMany(t => t.Methods))
                {
                    if (!m.HasBody)
                        continue;
            
                    foreach (var inst in m.Body.Instructions.Where(i => i.OpCode == OpCodes.Call))
                    {
                        var calledMethod = ((MethodReference) inst.Operand);
//                        var p = problemDefinitions.SingleOrDefault(c => c.type.Name == calledMethod.DeclaringType.Name);
                        var p = problemDefinitions.SingleOrDefault(c => c.type == calledMethod.DeclaringType.Name);
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
                                m_ProjectIssues.Add(new ProjectIssue
                                {
                                    category = "API Call",
                                    def = p,
                                    url = s.Document.Url,
                                    line = s.StartLine,
                                    column = s.StartColumn
                                });
                                //                                Debug.Log($"Issue is {msg} {s.Document.Url} ({s.StartLine},{s.StartColumn})");
                            }
            
                        }
                    }
                }
            }
        }

        void AnalyzeAssembly(List<ProblemDefinition> problemDefinitions, Assembly assembly)
        {
            foreach (var p in problemDefinitions)
            {
                try
                {
                    var value = MethodEvaluator.Eval(assembly.Location,
                        p.type, "get_" + p.method, new System.Type[0]{}, new object[0]{});

                    if (value.ToString() == p.value)
                        m_ProjectIssues.Add(new ProjectIssue
                        {
                            category = "ProjectSettings",
                            def = p,
                            url = "N/A"
                        });
                }
                catch (Exception e)
                {
                    // TODO
                }
            }
            
        }
        
        public void AnalyzeProjectSettings(List<ProblemDefinition> problemDefinitions)
        {
            string [] assemblyNames = new string[]{"UnityEditor.dll", "UnityEngine.dll"}; 
            var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(x => assemblyNames.Contains(x.ManifestModule.Name));

            foreach (var assembly in assemblies)
            {
                AnalyzeAssembly(problemDefinitions, assembly);
            }
        }

        public void WriteToFile()
        {
            string json = JsonHelper.ToJson<ProjectIssue>(m_ProjectIssues.ToArray(), true);
            File.WriteAllText("Report.json", json);
        }
    }
}