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
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using ThreadPriority = System.Threading.ThreadPriority;

namespace Unity.ProjectAuditor.Editor.Auditors
{
    public enum AssemblyProperty
    {
        ReadOnly = 0,
        Num
    }

    public enum CodeProperty
    {
        Assembly = 0,
        Num
    }

    public enum CompilerMessageProperty
    {
        Code = 0,
        Assembly,
        Num
    }

    class CodeModule : ProjectAuditorModule
    {
        static readonly ProblemDescriptor k_AssemblyDescriptor = new ProblemDescriptor
            (
            700001,
            "Assembly"
            );

        const int k_CompilerMessageFirstId = 800000;

        static readonly IssueLayout k_AssemblyLayout = new IssueLayout
        {
            category = IssueCategory.Assembly,
            properties = new[]
            {
                new PropertyDefinition { type = PropertyType.Description, name = "Assembly Name"},
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AssemblyProperty.ReadOnly), format = PropertyFormat.Bool, name = "Read Only"},
                new PropertyDefinition { type = PropertyType.Path, name = "asmdef Path"},
            }
        };

        static readonly IssueLayout k_IssueLayout = new IssueLayout
        {
            category = IssueCategory.Code,
            properties = new[]
            {
                new PropertyDefinition { type = PropertyType.Description, name = "Issue", longName = "Issue description"},
                new PropertyDefinition { type = PropertyType.CriticalContext, format = PropertyFormat.Bool, name = "Critical", longName = "Critical code path"},
                new PropertyDefinition { type = PropertyType.Area, name = "Area", longName = "The area the issue might have an impact on"},
                new PropertyDefinition { type = PropertyType.Filename, name = "Filename", longName = "Filename and line number"},
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(CodeProperty.Assembly), format = PropertyFormat.String, name = "Assembly", longName = "Managed Assembly name" }
            }
        };

        static readonly IssueLayout k_CompilerMessageLayout = new IssueLayout
        {
            category = IssueCategory.CodeCompilerMessage,
            properties = new[]
            {
                new PropertyDefinition { type = PropertyType.Severity, name = "Type"},
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(CompilerMessageProperty.Code), format = PropertyFormat.String, name = "Code"},
                new PropertyDefinition { type = PropertyType.Description, format = PropertyFormat.String, name = "Message", longName = "Compiler Message"},
                new PropertyDefinition { type = PropertyType.Filename, name = "Filename", longName = "Filename and line number"},
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(CompilerMessageProperty.Assembly), format = PropertyFormat.String, name = "Target Assembly", longName = "Managed Assembly name" },
                new PropertyDefinition { type = PropertyType.Path, name = "Full path"},
            }
        };

        static readonly IssueLayout k_GenericIssueLayout = new IssueLayout
        {
            category = IssueCategory.GenericInstance,
            properties = new[]
            {
                new PropertyDefinition { type = PropertyType.Description, name = "Generic Type"},
                new PropertyDefinition { type = PropertyType.Filename, name = "Filename", longName = "Filename and line number"},
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(CodeProperty.Assembly), format = PropertyFormat.String, name = "Assembly", longName = "Managed Assembly name" }
            }
        };

        ProjectAuditorConfig m_Config;
        List<IInstructionAnalyzer> m_Analyzers;
        List<OpCode> m_OpCodes;
        List<ProblemDescriptor> m_ProblemDescriptors;
        Dictionary<string, ProblemDescriptor> m_RuntimeDescriptors = new Dictionary<string, ProblemDescriptor>();

        Thread m_AssemblyAnalysisThread;

        public override IEnumerable<ProblemDescriptor> GetDescriptors()
        {
            return m_ProblemDescriptors;
        }

        public override IEnumerable<IssueLayout> GetLayouts()
        {
            yield return k_AssemblyLayout;
            yield return k_IssueLayout;
            yield return k_CompilerMessageLayout;
            yield return k_GenericIssueLayout;
        }

        public override void Initialize(ProjectAuditorConfig config)
        {
            m_Config = config;
            m_Analyzers = new List<IInstructionAnalyzer>();
            m_OpCodes = new List<OpCode>();
            m_ProblemDescriptors = new List<ProblemDescriptor>();

            foreach (var type in TypeCache.GetTypesDerivedFrom(typeof(IInstructionAnalyzer)))
                AddAnalyzer(Activator.CreateInstance(type) as IInstructionAnalyzer);
        }

        public override void Audit(Action<ProjectIssue> onIssueFound, Action onComplete = null, IProgress progress = null)
        {
            if (m_ProblemDescriptors == null)
                throw new Exception("Descriptors Database not initialized.");

            if (m_Config.AnalyzeInBackground && m_AssemblyAnalysisThread != null)
                m_AssemblyAnalysisThread.Join();

            var compilationPipeline = new AssemblyCompilationPipeline
            {
                AssemblyCompilationFinished = (assemblyName, compilerMessages) => ProcessCompilerMessages(assemblyName, compilerMessages, onIssueFound)
            };

            Profiler.BeginSample("CodeModule.Audit.Compilation");
            var assemblyInfos = compilationPipeline.Compile(m_Config.AnalyzeEditorCode, progress);
            Profiler.EndSample();

            var callCrawler = new CallCrawler();
            var issues = new List<ProjectIssue>();
            var localAssemblyInfos = assemblyInfos.Where(info => !info.readOnly).ToArray();
            var readOnlyAssemblyInfos = assemblyInfos.Where(info => info.readOnly).ToArray();

            foreach (var assemblyInfo in assemblyInfos)
            {
                onIssueFound(new ProjectIssue(k_AssemblyDescriptor, assemblyInfo.name, IssueCategory.Assembly, assemblyInfo.asmDefPath,
                    new object[(int)AssemblyProperty.Num]
                    {
                        assemblyInfo.readOnly
                    }));
            }

            var assemblyDirectories = new List<string>();
            assemblyDirectories.AddRange(AssemblyInfoProvider.GetPrecompiledAssemblyDirectories(PrecompiledAssemblyTypes.UserAssembly | PrecompiledAssemblyTypes.UnityEngine));
            if (m_Config.AnalyzeEditorCode)
                assemblyDirectories.AddRange(AssemblyInfoProvider.GetPrecompiledAssemblyDirectories(PrecompiledAssemblyTypes.UnityEditor));

            var onCallFound = new Action<CallInfo>(pair =>
            {
                callCrawler.Add(pair);
            });

            var onCompleteInternal = new Action<IProgress>(bar =>
            {
                Profiler.BeginSample("CodeModule.Audit.BuildCallHierarchies");
                compilationPipeline.Dispose();
                callCrawler.BuildCallHierarchies(issues, bar);
                Profiler.EndSample();

                if (onComplete != null)
                    onComplete();
            });

            var onIssueFoundInternal = new Action<ProjectIssue>(issue =>
            {
                if (issue.category == IssueCategory.Code)
                    issues.Add(issue);
                onIssueFound(issue);
            });

            Profiler.BeginSample("CodeModule.Audit.Analysis");

            // first phase: analyze assemblies generated from editable scripts
            AnalyzeAssemblies(localAssemblyInfos, assemblyDirectories, onCallFound, onIssueFoundInternal, null, progress);

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
                Profiler.BeginSample("CodeModule.Audit.AnalysisReadOnly");
                AnalyzeAssemblies(readOnlyAssemblyInfos, assemblyDirectories, onCallFound, onIssueFoundInternal, onCompleteInternal, progress);
                Profiler.EndSample();
            }
            Profiler.EndSample();
        }

        void AnalyzeAssemblies(IEnumerable<AssemblyInfo> assemblyInfos, List<string> assemblyDirectories, Action<CallInfo> onCallFound, Action<ProjectIssue> onIssueFound, Action<IProgress> onComplete, IProgress progress = null)
        {
            using (var assemblyResolver = new DefaultAssemblyResolver())
            {
                foreach (var path in assemblyDirectories)
                    assemblyResolver.AddSearchDirectory(path);

                foreach (var dir in assemblyInfos.Select(info => Path.GetDirectoryName(info.path)).Distinct())
                    assemblyResolver.AddSearchDirectory(dir);

                if (progress != null)
                    progress.Start("Analyzing Assemblies", string.Empty, assemblyInfos.Count());

                // Analyse all Player assemblies
                foreach (var assemblyInfo in assemblyInfos)
                {
                    Console.WriteLine("[Project Auditor] Analyzing {0}", assemblyInfo.name);
                    if (progress != null)
                        progress.Advance(assemblyInfo.name);

                    if (!File.Exists(assemblyInfo.path))
                    {
                        Debug.LogError(assemblyInfo.path + " not found.");
                        continue;
                    }

                    AnalyzeAssembly(assemblyInfo, assemblyResolver, onCallFound, onIssueFound);
                }
            }

            if (progress != null)
                progress.Clear();

            if (onComplete != null)
                onComplete(progress);
        }

        public override void RegisterDescriptor(ProblemDescriptor descriptor)
        {
            // TODO: check for id conflict
            m_ProblemDescriptors.Add(descriptor);
        }

        void AnalyzeAssembly(AssemblyInfo assemblyInfo, IAssemblyResolver assemblyResolver, Action<CallInfo> onCallFound, Action<ProjectIssue> onIssueFound)
        {
            Profiler.BeginSample("CodeModule.Analyze " + assemblyInfo.name);

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

            Profiler.BeginSample("CodeModule.AnalyzeMethodBody");

            var callerNode = new CallTreeNode(caller);

            Profiler.BeginSample("CodeModule.IsPerformanceCriticalContext");
            var perfCriticalContext = IsPerformanceCriticalContext(caller);
            Profiler.EndSample();

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
                    Profiler.BeginSample("CodeModule.OnCallFound");
                    onCallFound(new CallInfo(
                        (MethodReference)inst.Operand,
                        caller,
                        location,
                        perfCriticalContext
                    ));
                    Profiler.EndSample();
                }

                Profiler.BeginSample("CodeModule.Analyzing " + inst.OpCode.Name);
                foreach (var analyzer in m_Analyzers)
                    if (analyzer.GetOpCodes().Contains(inst.OpCode))
                    {
                        Profiler.BeginSample("CodeModule " + analyzer.GetType().Name);
                        var projectIssue = analyzer.Analyze(caller, inst);
                        if (projectIssue != null)
                        {
                            projectIssue.dependencies.perfCriticalContext = perfCriticalContext;
                            projectIssue.dependencies.AddChild(callerNode);
                            projectIssue.location = location;
                            projectIssue.SetCustomProperties(new string[(int)CodeProperty.Num] {assemblyInfo.name});

                            onIssueFound(projectIssue);
                        }
                        Profiler.EndSample();
                    }
                Profiler.EndSample();
            }
            Profiler.EndSample();
        }

        void AddAnalyzer(IInstructionAnalyzer analyzer)
        {
            analyzer.Initialize(this);
            m_Analyzers.Add(analyzer);
            m_OpCodes.AddRange(analyzer.GetOpCodes());
        }

        void ProcessCompilerMessages(string assemblyName, CompilerMessage[] compilerMessages, Action<ProjectIssue> onIssueFound)
        {
            Profiler.BeginSample("CodeModule.ProcessCompilerMessages");

            foreach (var message in compilerMessages)
            {
                var descriptor = (ProblemDescriptor)null;

                if (m_RuntimeDescriptors.ContainsKey(message.code))
                    descriptor = m_RuntimeDescriptors[message.code];
                else
                {
                    var severity = Rule.Severity.Info;
                    switch (message.type)
                    {
                        case CompilerMessageType.Error:
                            severity = Rule.Severity.Error;
                            break;
                        case CompilerMessageType.Warning:
                            severity = Rule.Severity.Warning;
                            break;
                        case CompilerMessageType.Info:
                            severity = Rule.Severity.Info;
                            break;
                    }
                    descriptor = new ProblemDescriptor
                        (
                        k_CompilerMessageFirstId + m_RuntimeDescriptors.Count,
                        message.code
                        )
                    {
                        severity = severity
                    };
                    m_RuntimeDescriptors.Add(message.code, descriptor);
                }

                var issue = new ProjectIssue(descriptor, message.message,
                    IssueCategory.CodeCompilerMessage,
                    new Location(message.file.Replace("\\", "/"), message.line),
                    new object[(int)CompilerMessageProperty.Num]
                    {
                        message.code,
                        assemblyName
                    });
                onIssueFound(issue);
            }

            Profiler.EndSample();
        }

        static bool IsPerformanceCriticalContext(MethodDefinition methodDefinition)
        {
            if (MonoBehaviourAnalysis.IsMonoBehaviour(methodDefinition.DeclaringType) &&
                MonoBehaviourAnalysis.IsMonoBehaviourUpdateMethod(methodDefinition))
                return true;
#if ENTITIES_PACKAGE_INSTALLED
            if (ComponentSystemAnalysis.IsComponentSystem(methodDefinition.DeclaringType) &&
                ComponentSystemAnalysis.IsOnUpdateMethod(methodDefinition))
                return true;
#endif
            return false;
        }
    }
}
