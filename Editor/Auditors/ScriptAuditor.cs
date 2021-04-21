using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.ProjectAuditor.Editor.CodeAnalysis;
using Unity.ProjectAuditor.Editor.InstructionAnalyzers;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor.Compilation;
using UnityEngine;
using UnityEngine.Profiling;
using ThreadPriority = System.Threading.ThreadPriority;

namespace Unity.ProjectAuditor.Editor.Auditors
{
    public enum CodeProperty
    {
        Assembly = 0,
        Num
    }

    class ScriptAuditor : IAuditor
    {
        const int k_CompilerMessageFirstId = 800000;

        static readonly IssueLayout k_IssueLayout = new IssueLayout
        {
            category = IssueCategory.Code,
            properties = new[]
            {
                new PropertyDefinition { type = PropertyType.Description, name = "Issue", longName = "Issue description"},
                new PropertyDefinition { type = PropertyType.CriticalContext, format = PropertyFormat.Bool, name = "Critical", longName = "Critical code path"},
                new PropertyDefinition { type = PropertyType.Area, name = "Area", longName = "The area the issue might have an impact on"},
                new PropertyDefinition { type = PropertyType.Filename, name = "Filename", longName = "Filename and line number"},
                new PropertyDefinition { type = PropertyType.Custom, format = PropertyFormat.String, name = "Assembly", longName = "Managed Assembly name" }
            }
        };

        static readonly IssueLayout k_CompilerMessageLayout = new IssueLayout
        {
            category = IssueCategory.CodeCompilerMessages,
            properties = new[]
            {
                new PropertyDefinition { type = PropertyType.Description, format = PropertyFormat.String, name = "Message", longName = "Compiler Message"},
                new PropertyDefinition { type = PropertyType.Severity, name = "Type"},
                new PropertyDefinition { type = PropertyType.Filename, name = "Filename", longName = "Filename and line number"},
                new PropertyDefinition { type = PropertyType.Custom, format = PropertyFormat.String, name = "Target Assembly", longName = "Managed Assembly name" },
                new PropertyDefinition { type = PropertyType.Path, name = "Full path"},
            }
        };

        static readonly IssueLayout k_GenericIssueLayout = new IssueLayout
        {
            category = IssueCategory.Generics,
            properties = new[]
            {
                new PropertyDefinition { type = PropertyType.Description, name = "Generic Type"},
                new PropertyDefinition { type = PropertyType.Filename, name = "Filename", longName = "Filename and line number"},
                new PropertyDefinition { type = PropertyType.Custom, format = PropertyFormat.String, name = "Assembly", longName = "Managed Assembly name" }
            }
        };

        ProjectAuditorConfig m_Config;
        List<IInstructionAnalyzer> m_Analyzers;
        List<OpCode> m_OpCodes;
        List<ProblemDescriptor> m_ProblemDescriptors;

        Thread m_AssemblyAnalysisThread;

        public IEnumerable<ProblemDescriptor> GetDescriptors()
        {
            return m_ProblemDescriptors;
        }

        public IEnumerable<IssueLayout> GetLayouts()
        {
            yield return k_IssueLayout;
            yield return k_CompilerMessageLayout;
            yield return k_GenericIssueLayout;
        }

        public void Initialize(ProjectAuditorConfig config)
        {
            m_Config = config;
            m_Analyzers = new List<IInstructionAnalyzer>();
            m_OpCodes = new List<OpCode>();
            m_ProblemDescriptors = new List<ProblemDescriptor>();

            foreach (var type in TypeInfo.GetAllTypesInheritedFromInterface<IInstructionAnalyzer>())
                AddAnalyzer(Activator.CreateInstance(type) as IInstructionAnalyzer);
        }

        public bool IsSupported()
        {
            return true;
        }

        public void Audit(Action<ProjectIssue> onIssueFound, Action onComplete, IProgressBar progressBar = null)
        {
            if (m_ProblemDescriptors == null)
                throw new Exception("Issue Database not initialized.");

            if (m_Config.AnalyzeInBackground && m_AssemblyAnalysisThread != null)
                m_AssemblyAnalysisThread.Join();

            var compilerMessageId = k_CompilerMessageFirstId;
            var descriptorDictionary = new Dictionary<string, ProblemDescriptor>();
            var compilationPipeline = new AssemblyCompilationPipeline
            {
                AssemblyCompilationFinished = (assemblyName, compilerMessages) =>
                {
                    foreach (var message in compilerMessages)
                    {
                        var messageStartIndex = message.message.IndexOf(":");
                        if (messageStartIndex != -1)
                        {
                            var messageWithCode = message.message.Substring(messageStartIndex + 2);
                            var messageCodeStartIndex = messageWithCode.IndexOf("CS");
                            if (messageCodeStartIndex == -1)
                                continue; // for the time being, skip any other message

                            var messageCode = messageWithCode.Substring(messageCodeStartIndex, 6);
                            var messageDescription = messageWithCode.Substring(messageWithCode.IndexOf(": ") + 2);
                            var descriptor = (ProblemDescriptor)null;

                            if (descriptorDictionary.ContainsKey(messageCode))
                                descriptor = descriptorDictionary[messageCode];
                            else
                            {
                                descriptor = new ProblemDescriptor
                                    (
                                    compilerMessageId++,
                                    messageCode,
                                    Area.CPU
                                    )
                                {
                                    severity = message.type == CompilerMessageType.Error
                                        ? Rule.Severity.Error
                                        : Rule.Severity.Warning
                                };
                                descriptorDictionary.Add(messageCode, descriptor);
                            }

                            var issue = new ProjectIssue(descriptor, messageDescription,
                                IssueCategory.CodeCompilerMessages,
                                new Location(message.file, message.line),
                                new[]
                                {
                                    assemblyName
                                });
                            onIssueFound(issue);
                        }
                    }
                }
            };

            Profiler.BeginSample("ScriptAuditor.Audit.Compilation");
            var assemblyInfos = compilationPipeline.Compile(m_Config.AnalyzeEditorCode, progressBar);
            Profiler.EndSample();

            var callCrawler = new CallCrawler();
            var issues = new List<ProjectIssue>();
            var localAssemblyInfos = assemblyInfos.Where(info => !info.readOnly).ToArray();
            var readOnlyAssemblyInfos = assemblyInfos.Where(info => info.readOnly).ToArray();

            var assemblyDirectories = new List<string>();
            assemblyDirectories.AddRange(AssemblyInfoProvider.GetPrecompiledAssemblyDirectories(PrecompiledAssemblyTypes.UserAssembly | PrecompiledAssemblyTypes.UnityEngine));
            if (m_Config.AnalyzeEditorCode)
                assemblyDirectories.AddRange(AssemblyInfoProvider.GetPrecompiledAssemblyDirectories(PrecompiledAssemblyTypes.UnityEditor));

            var onCallFound = new Action<CallInfo>(pair =>
            {
                callCrawler.Add(pair);
            });

            var onCompleteInternal = new Action<IProgressBar>(bar =>
            {
                compilationPipeline.Dispose();
                callCrawler.BuildCallHierarchies(issues, bar);
                onComplete();
            });

            var onIssueFoundInternal = new Action<ProjectIssue>(issue =>
            {
                if (issue.category == IssueCategory.Code)
                    issues.Add(issue);
                onIssueFound(issue);
            });

            Profiler.BeginSample("ScriptAuditor.Audit.Analysis");

            // first phase: analyze assemblies generated from editable scripts
            AnalyzeAssemblies(localAssemblyInfos, assemblyDirectories, onCallFound, onIssueFoundInternal, null, progressBar);

            var enableBackgroundAnalysis = m_Config.AnalyzeInBackground;
#if !UNITY_2019_3_OR_NEWER
            enableBackgroundAnalysis = false;
#endif
            // second phase: analyze all remaining assemblies, in a separate thread if enableBackgroundAnalysis is enabled
            if (enableBackgroundAnalysis)
            {
                m_AssemblyAnalysisThread = new Thread(() =>
                    AnalyzeAssemblies(readOnlyAssemblyInfos, assemblyDirectories, onCallFound, onIssueFoundInternal, onCompleteInternal));
                m_AssemblyAnalysisThread.Name = "Assembly Analysis";
                m_AssemblyAnalysisThread.Priority = ThreadPriority.BelowNormal;
                m_AssemblyAnalysisThread.Start();
            }
            else
            {
                Profiler.BeginSample("ScriptAuditor.Audit.AnalysisReadOnly");
                AnalyzeAssemblies(readOnlyAssemblyInfos, assemblyDirectories, onCallFound, onIssueFoundInternal, onCompleteInternal, progressBar);
                Profiler.EndSample();
            }
            Profiler.EndSample();
        }

        void AnalyzeAssemblies(IEnumerable<AssemblyInfo> assemblyInfos, List<string> assemblyDirectories, Action<CallInfo> onCallFound, Action<ProjectIssue> onIssueFound, Action<IProgressBar> onComplete, IProgressBar progressBar = null)
        {
            using (var assemblyResolver = new DefaultAssemblyResolver())
            {
                foreach (var path in assemblyDirectories)
                    assemblyResolver.AddSearchDirectory(path);

                foreach (var dir in assemblyInfos.Select(info => Path.GetDirectoryName(info.path)).Distinct())
                    assemblyResolver.AddSearchDirectory(dir);

                if (progressBar != null)
                    progressBar.Initialize("Analyzing Scripts", "Analyzing project scripts",
                        assemblyInfos.Count());

                // Analyse all Player assemblies
                foreach (var assemblyInfo in assemblyInfos)
                {
                    Console.WriteLine("[Project Auditor] Analyzing {0}", assemblyInfo.name);
                    if (progressBar != null)
                        progressBar.AdvanceProgressBar(string.Format("Analyzing {0}", assemblyInfo.name));

                    if (!File.Exists(assemblyInfo.path))
                    {
                        Debug.LogError(assemblyInfo.path + " not found.");
                        continue;
                    }

                    AnalyzeAssembly(assemblyInfo, assemblyResolver, onCallFound, onIssueFound);
                }
            }

            if (progressBar != null)
                progressBar.ClearProgressBar();

            if (onComplete != null)
            {
                onComplete(progressBar);
            }
        }

        public void RegisterDescriptor(ProblemDescriptor descriptor)
        {
            // TODO: check for id conflict
            m_ProblemDescriptors.Add(descriptor);
        }

        void AnalyzeAssembly(AssemblyInfo assemblyInfo, IAssemblyResolver assemblyResolver, Action<CallInfo> onCallFound, Action<ProjectIssue> onIssueFound)
        {
            Profiler.BeginSample("ScriptAuditor.AnalyzeAssembly " + assemblyInfo.name);

            using (var assembly = AssemblyDefinition.ReadAssembly(assemblyInfo.path,
                new ReaderParameters {ReadSymbols = true, AssemblyResolver = assemblyResolver}))
            {
                foreach (var methodDefinition in MonoCecilHelper.AggregateAllTypeDefinitions(assembly.MainModule.Types)
                         .SelectMany(t => t.Methods))
                {
                    if (!methodDefinition.HasBody)
                        continue;

                    AnalyzeMethodBody(assemblyInfo, methodDefinition, onCallFound, onIssueFound);
                }
            }

            Profiler.EndSample();
        }

        void AnalyzeMethodBody(AssemblyInfo assemblyInfo, MethodDefinition caller, Action<CallInfo> onCallFound, Action<ProjectIssue> onIssueFound)
        {
            if (!caller.DebugInformation.HasSequencePoints)
                return;

            Profiler.BeginSample("ScriptAuditor.AnalyzeMethodBody");

            var callerNode = new CallTreeNode(caller);
            var perfCriticalContext = IsPerformanceCriticalContext(caller);

            foreach (var inst in caller.Body.Instructions.Where(i => m_OpCodes.Contains(i.OpCode)))
            {
                SequencePoint s = null;
                for (var i = inst; i != null; i = i.Previous)
                {
                    s = caller.DebugInformation.GetSequencePoint(i);
                    if (s != null)
                        break;
                }

                Location location = null;
                if (s != null && !s.IsHidden)
                {
                    location = new Location(AssemblyInfoProvider.ResolveAssetPath(assemblyInfo, s.Document.Url), s.StartLine);
                    callerNode.location = location;
                }
                else
                {
                    // sequence point not found. Assuming caller.IsHideBySig == true
                }

                if (inst.OpCode == OpCodes.Call || inst.OpCode == OpCodes.Callvirt)
                {
                    onCallFound(new CallInfo
                    {
                        callee = (MethodReference)inst.Operand,
                        caller = caller,
                        location = location,
                        perfCriticalContext = perfCriticalContext
                    });
                }

                foreach (var analyzer in m_Analyzers)
                    if (analyzer.GetOpCodes().Contains(inst.OpCode))
                    {
                        var projectIssue = analyzer.Analyze(caller, inst);
                        if (projectIssue != null)
                        {
                            projectIssue.dependencies.perfCriticalContext = perfCriticalContext;
                            projectIssue.dependencies.AddChild(callerNode);
                            projectIssue.location = location;
                            projectIssue.SetCustomProperties(new[] {assemblyInfo.name});

                            onIssueFound(projectIssue);
                        }
                    }
            }
            Profiler.EndSample();
        }

        void AddAnalyzer(IInstructionAnalyzer analyzer)
        {
            analyzer.Initialize(this);
            m_Analyzers.Add(analyzer);
            m_OpCodes.AddRange(analyzer.GetOpCodes());
        }

        static bool IsPerformanceCriticalContext(MethodDefinition methodDefinition)
        {
            if (MonoBehaviourAnalysis.IsMonoBehaviour(methodDefinition.DeclaringType) &&
                MonoBehaviourAnalysis.IsMonoBehaviourUpdateMethod(methodDefinition))
                return true;
            if (ComponentSystemAnalysis.IsComponentSystem(methodDefinition.DeclaringType) &&
                ComponentSystemAnalysis.IsOnUpdateMethod(methodDefinition))
                return true;
            return false;
        }
    }
}
