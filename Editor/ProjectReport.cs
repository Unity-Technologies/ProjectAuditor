using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            // TODO: How do i make sure Assembly-CSharp.dll exists?
            Debug.Log("Analyzing project...");
            using (var a = AssemblyDefinition.ReadAssembly("Library/ScriptAssemblies/Assembly-CSharp.dll", new ReaderParameters() { ReadSymbols = true} ))
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

        public void AnalyzeProjectSettings(List<ProblemDefinition> problemDefinitions)
        {
            foreach (var def in problemDefinitions)
            {
                try
                {
                    var value = MethodEvaluator.Eval("/Applications/Unity/Hub/Editor/2018.4.4f1/Unity.app/Contents/Managed/UnityEditor.dll",
                        def.type, "get_" + def.method, new System.Type[0]{}, new object[0]{});

                    if (value.ToString() == def.value)
                        m_ProjectIssues.Add(new ProjectIssue
                        {
                            category = "ProjectSettings",
                            def = def,
                            url = def.type
                        });

                }
                catch (Exception e)
                {
                    var value = MethodEvaluator.Eval("/Applications/Unity/Hub/Editor/2018.4.4f1/Unity.app/Contents/Managed/UnityEngine/UnityEngine.dll",
                        def.type, "get_" + def.method, new System.Type[0]{}, new object[0]{});

                    if (value.ToString() == def.value)
                        m_ProjectIssues.Add(new ProjectIssue
                        {
                            category = "ProjectSettings",
                            def = def,
                            url = def.type
                        });

//                    throw;
                }
            }
        }

        public void WriteToFile()
        {
            string json = JsonHelper.ToJson<ProjectIssue>(m_ProjectIssues.ToArray(), true);
            File.WriteAllText("Report.json", json);
        }
    }
}