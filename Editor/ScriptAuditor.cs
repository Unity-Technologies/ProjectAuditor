using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using Mono.Cecil;
using Mono.Cecil.Cil;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

#if UNITY_2018_1_OR_NEWER
using UnityEditor.Build.Player;
#endif

namespace Unity.ProjectAuditor.Editor
{
    internal class MonoCecilHelper
    {
        public static IEnumerable<TypeDefinition> AggregateAllTypeDefinitions(IEnumerable<TypeDefinition> types)
        {
            var typeDefs = types.ToList();
            foreach (var typeDefinition in types)
            {
                if (typeDefinition.HasNestedTypes)
                    typeDefs.AddRange(AggregateAllTypeDefinitions(typeDefinition.NestedTypes));
            }
            return typeDefs;
        }
    }

    public class ScriptAuditor : IAuditor
    {
        private List<ProblemDescriptor> m_ProblemDescriptors;
        private ProblemDescriptor[] m_ProblemsDefinedByOpCopde;
        
        private UnityEditor.Compilation.Assembly[] m_PlayerAssemblies;

        private string[] m_AssemblyNames;
                
        public string[] assemblyNames
        {
            get
            {
                if (m_AssemblyNames != null)
                    return m_AssemblyNames;

                List<string> names = new List<string>();
                foreach (var assembly in m_PlayerAssemblies)
                {
                    names.Add(assembly.name);                    
                }

                m_AssemblyNames = names.ToArray();
                return m_AssemblyNames;
            }
        }   
        
        public ScriptAuditor()
        {
#if UNITY_2018_1_OR_NEWER
            m_PlayerAssemblies = CompilationPipeline.GetAssemblies(AssembliesType.Player);
#else
            m_PlayerAssemblies = CompilationPipeline.GetAssemblies().Where(a => a.flags != AssemblyFlags.EditorAssembly).ToArray();
#endif
        }

        public string GetUIName()
        {
            return "Scripts";
        }

        public void Audit( ProjectReport projectReport, ProjectAuditorConfig config)
        {
            var assemblies = GetPlayerAssemblies();
            if (assemblies.Count > 0)
            {
                var callCrawler = new CallCrawler();                
                
                var progressBar =
                    new ProgressBarDisplay("Analyzing Scripts", "Analyzing project scripts", m_PlayerAssemblies.Length);

                // Analyse all Player assemblies, including Package assemblies.
                foreach (var assemblyPath in assemblies)
                {
                    progressBar.AdvanceProgressBar(string.Format("Analyzing {0} scripts", Path.GetFileName(assemblyPath)));

                    if (!File.Exists(assemblyPath))
                    {
                        Debug.LogError(assemblyPath + " not found.");
                        continue;
                    }
                    
                    AnalyzeAssembly(assemblyPath, projectReport, config, callCrawler);
                }
                progressBar.ClearProgressBar();

                callCrawler.BuildCallHierarchies(projectReport);
            }            
        }
        
        private List<string> GetPlayerAssemblies()
        {
            List<string> assemblies = new List<string>();  
#if UNITY_2018_2_OR_NEWER
            var outputFolder = FileUtil.GetUniqueTempPathInProject();
            if (Directory.Exists(outputFolder))
                Directory.Delete(outputFolder, true);

            ScriptCompilationSettings input = new ScriptCompilationSettings();
            input.target = EditorUserBuildSettings.activeBuildTarget;
            input.@group = EditorUserBuildSettings.selectedBuildTargetGroup;

            var compilationResult = PlayerBuildInterface.CompilePlayerScripts(input, outputFolder);
            foreach (var assembly in compilationResult.assemblies)
            {
                assemblies.Add(Path.Combine(outputFolder, assembly));    
            }
#else
            // fallback to CompilationPipeline assemblies 
            foreach (var playerAssembly in m_PlayerAssemblies)
            {
                assemblies.Add(playerAssembly.outputPath);                   
            }   
#endif
            return assemblies;
        }

        private void AnalyzeAssembly(string assemblyPath, ProjectReport projectReport, ProjectAuditorConfig config, CallCrawler callCrawler)
        {
            using (var a = AssemblyDefinition.ReadAssembly(assemblyPath, new ReaderParameters() {ReadSymbols = true}))
            {
                foreach (var m in MonoCecilHelper.AggregateAllTypeDefinitions(a.MainModule.Types).SelectMany(t => t.Methods))
                {
                    if (!m.HasBody)
                        continue;

                    AnalyzeMethodBody(projectReport, config, a, m, callCrawler);
                }     
            }
        }

        private void AnalyzeMethodBody(ProjectReport projectReport, ProjectAuditorConfig config, AssemblyDefinition a, MethodDefinition caller, CallCrawler callCrawler)
        {
            if (!caller.DebugInformation.HasSequencePoints)
                return;
            
            List<ProjectIssue> methodBobyIssues = new List<ProjectIssue>();

            foreach (var inst in caller.Body.Instructions.Where(i =>
                (i.OpCode == OpCodes.Call || i.OpCode == OpCodes.Callvirt || i.OpCode == OpCodes.Box)))
            {
                //var msg = string.Empty;
                SequencePoint s = null;
                for (var i = inst; i != null; i = i.Previous)
                {
                    s = caller.DebugInformation.GetSequencePoint(i);
                    if (s != null)
                    {
                        // msg = i == inst ? " exactly" : "nearby";
                        break;
                    }
                }
                
                ProblemDescriptor descriptor = null;
                var description = (descriptor != null) ? descriptor.description : string.Empty;
                CallTreeNode callTree = null;
                if (inst.OpCode == OpCodes.Call || inst.OpCode == OpCodes.Callvirt)
                {
                    var callee = ((MethodReference) inst.Operand);

                    callCrawler.Add(caller, callee);

                    descriptor = m_ProblemDescriptors.SingleOrDefault(c => c.type == callee.DeclaringType.FullName &&
                                                                           (c.method == callee.Name ||
                                                                            ("get_" + c.method) == callee.Name));

                    if (descriptor == null)
                    {
                        // Are we trying to warn about a whole namespace?
                        descriptor = m_ProblemDescriptors.SingleOrDefault(c =>
                            c.type == callee.DeclaringType.Namespace && c.method == "*");
                    }
                    
                    if (description.Contains(".*"))
                    {
                        description = callee.DeclaringType.FullName + "::" + callee.Name;
                    }

                    // replace root with callee node
                    callTree = new CallTreeNode(callee, new CallTreeNode(caller));
                }
                else
                {
                    string opcode = inst.OpCode.Code.ToString();
                    descriptor = m_ProblemsDefinedByOpCopde.SingleOrDefault(p => p.opcode.Equals(opcode));
                    if (descriptor == null)
                    {
                        continue;
                    }
                    callTree = new CallTreeNode(opcode, new CallTreeNode(caller));
                }

                if (descriptor != null)
                {
                    // do not add the same type of issue again (for example multiple Linq instructions) 
                    if (methodBobyIssues.FirstOrDefault(i =>
                        i.column == s.StartColumn) == null)
                    {
                        var projectIssue = new ProjectIssue
                        {
                            description = description,
                            category = IssueCategory.ApiCalls,
                            descriptor = descriptor,
                            callTree = callTree,
                            url = s.Document.Url.Replace("\\", "/"),
                            line = s.StartLine,
                            column = s.StartColumn,
                            assembly = a.Name.Name
                        };

                        projectReport.AddIssue(projectIssue);
                        methodBobyIssues.Add(projectIssue);   
                    }
                }
            }
        }

        public void LoadDatabase(string path)
        {
            m_ProblemDescriptors = ProblemDescriptorHelper.LoadProblemDescriptors(path, "ApiDatabase");

            var descriptors = new List<ProblemDescriptor>();
            descriptors.Add(new ProblemDescriptor
            {
                id = 102000,
                opcode = OpCodes.Box.Code.ToString(),
                type = string.Empty,
                method = string.Empty,
                area = "Memory",
                problem = "Boxing happens where a value type, such as an integer, is converted into an object of reference type. This causes an allocation on the heap, which might increase the size of the managed heap and the frequency of Garbage Collection.",
                solution = "Try to avoid Boxing when possible.",
            });

            m_ProblemsDefinedByOpCopde = descriptors.ToArray();
            m_ProblemDescriptors.Where(p => !string.IsNullOrEmpty(p.opcode)).ToArray();                        
        }        
    }
}
