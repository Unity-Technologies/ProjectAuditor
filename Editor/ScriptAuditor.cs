using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    public class ScriptAuditor : IAuditor
    {
        private List<ProblemDescriptor> m_ProblemDescriptors;
        
        private UnityEditor.Compilation.Assembly[] m_PlayerAssemblies;

        private string[] m_WhitelistedPackages;
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
                        return;
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
                foreach (var m in a.MainModule.Types.SelectMany(t => t.Methods))
                {
                    if (!m.HasBody)
                        continue;

                    AnalyzeMethodBody(projectReport, config, a, m, callCrawler);
                }
            }
        }

        private void AnalyzeMethodBody(ProjectReport projectReport, ProjectAuditorConfig config, AssemblyDefinition a, MethodDefinition m, CallCrawler callCrawler)
        {
            List<ProjectIssue> methodBobyIssues = new List<ProjectIssue>();

            foreach (var inst in m.Body.Instructions.Where(i =>
                (i.OpCode == OpCodes.Call || i.OpCode == OpCodes.Callvirt)))
            {
                var calledMethod = ((MethodReference) inst.Operand);

                var callee = calledMethod.FullName;
                var caller = m.FullName;

                callCrawler.Add(caller, callee);
                
                // HACK: need to figure out a way to know whether a method is actually a property
                var descriptor = m_ProblemDescriptors.SingleOrDefault(c => c.type == calledMethod.DeclaringType.FullName &&
                                                                  (c.method == calledMethod.Name ||
                                                                   ("get_" + c.method) == calledMethod.Name));

                if (descriptor == null)
                {
                    // Are we trying to warn about a whole namespace?
                    descriptor = m_ProblemDescriptors.SingleOrDefault(c =>
                        c.type == calledMethod.DeclaringType.Namespace && c.method == "*");
                }

                //if (p.type != null && m.HasCustomDebugInformations)
                if (descriptor != null && m.DebugInformation.HasSequencePoints)
                {
                    //var msg = string.Empty;
                    SequencePoint s = null;
                    for (var i = inst; i != null; i = i.Previous)
                    {
                        s = m.DebugInformation.GetSequencePoint(i);
                        if (s != null)
                        {
                            // msg = i == inst ? " exactly" : "nearby";
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
                            var description = descriptor.description;
                            if (description.Contains(".*"))
                            {
                                description = calledMethod.DeclaringType.FullName + "::" + calledMethod.Name;
                            }

                            // do not add the same type of issue again (for example multiple Linq instructions) 
                            var foundIssues = methodBobyIssues.Where(i =>
                                i.column == s.StartColumn);
                            if (foundIssues.FirstOrDefault() == null)
                            {
                                var projectIssue = new ProjectIssue
                                {
                                    description = description,
                                    category = IssueCategory.ApiCalls,
                                    descriptor = descriptor,
                                    callTree = new CallTreeNode(callee, new CallTreeNode(caller)),
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
            }
        }

        public void LoadDatabase(string path)
        {
            m_ProblemDescriptors = ProblemDescriptorHelper.LoadProblemDescriptors(path, "ApiDatabase");
                        
            SetupPackageWhitelist(path);
        }        

        void SetupPackageWhitelist(string path)
        {
            var fullPath = Path.GetFullPath(Path.Combine(path, "PackageWhitelist.txt"));
            var whitelist = File.ReadAllText(fullPath);
            m_WhitelistedPackages = whitelist.Replace("\r\n", "\n").Split('\n');
        }
    }
}
