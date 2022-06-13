using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.ProjectAuditor.Editor.AssemblyUtils;
using Unity.ProjectAuditor.Editor.CodeAnalysis;
using Unity.ProjectAuditor.Editor.InstructionAnalyzers;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using Object = System.Object;
using ThreadPriority = System.Threading.ThreadPriority;

namespace Unity.ProjectAuditor.Editor.Modules
{
    public enum AssemblyProperty
    {
        ReadOnly = 0,
        CompileTime,
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
        class AssemblyAnalysisResult
        {
            public List<CallInfo> callInfos = new List<CallInfo>();
            public List<ProjectIssue> issues = new List<ProjectIssue>();
        }

        static readonly IssueLayout k_AssemblyLayout = new IssueLayout
        {
            category = IssueCategory.Assembly,
            properties = new[]
            {
                new PropertyDefinition {type = PropertyType.Severity},
                new PropertyDefinition {type = PropertyType.Description, name = "Assembly Name"},
                new PropertyDefinition
                {
                    type = PropertyTypeUtil.FromCustom(AssemblyProperty.CompileTime), format = PropertyFormat.Time,
                    name = "Compile Time (seconds)"
                },
                new PropertyDefinition
                {
                    type = PropertyTypeUtil.FromCustom(AssemblyProperty.ReadOnly), format = PropertyFormat.Bool,
                    name = "Read Only", defaultGroup = true
                },
                new PropertyDefinition {type = PropertyType.Path, name = "asmdef Path"},
            }
        };

        static readonly IssueLayout k_IssueLayout = new IssueLayout
        {
            category = IssueCategory.Code,
            properties = new[]
            {
                new PropertyDefinition
                {type = PropertyType.Description, name = "Issue", longName = "Issue description"},
                new PropertyDefinition
                {
                    type = PropertyType.CriticalContext, format = PropertyFormat.Bool, name = "Critical",
                    longName = "Critical code path"
                },
                new PropertyDefinition
                {type = PropertyType.Area, name = "Area", longName = "The area the issue might have an impact on"},
                new PropertyDefinition
                {type = PropertyType.Filename, name = "Filename", longName = "Filename and line number"},
                new PropertyDefinition
                {
                    type = PropertyTypeUtil.FromCustom(CodeProperty.Assembly), format = PropertyFormat.String,
                    name = "Assembly", longName = "Managed Assembly name"
                },
                new PropertyDefinition {type = PropertyType.Descriptor, name = "Descriptor", defaultGroup = true},
            }
        };

        static readonly IssueLayout k_CompilerMessageLayout = new IssueLayout
        {
            category = IssueCategory.CodeCompilerMessage,
            properties = new[]
            {
                new PropertyDefinition {type = PropertyType.Severity, name = "Type"},
                new PropertyDefinition
                {
                    type = PropertyTypeUtil.FromCustom(CompilerMessageProperty.Code), format = PropertyFormat.String,
                    name = "Code", defaultGroup = true
                },
                new PropertyDefinition
                {
                    type = PropertyType.Description, format = PropertyFormat.String, name = "Message",
                    longName = "Compiler Message"
                },
                new PropertyDefinition
                {type = PropertyType.Filename, name = "Filename", longName = "Filename and line number"},
                new PropertyDefinition
                {
                    type = PropertyTypeUtil.FromCustom(CompilerMessageProperty.Assembly),
                    format = PropertyFormat.String, name = "Target Assembly", longName = "Managed Assembly name"
                },
                new PropertyDefinition {type = PropertyType.Path, name = "Full path"},
            }
        };

        static readonly IssueLayout k_GenericIssueLayout = new IssueLayout
        {
            category = IssueCategory.GenericInstance,
            properties = new[]
            {
                new PropertyDefinition {type = PropertyType.Description, name = "Generic Type"},
                new PropertyDefinition
                {type = PropertyType.Filename, name = "Filename", longName = "Filename and line number"},
                new PropertyDefinition
                {
                    type = PropertyTypeUtil.FromCustom(CodeProperty.Assembly), format = PropertyFormat.String,
                    name = "Assembly", longName = "Managed Assembly name"
                }
            }
        };

        ProjectAuditorConfig m_Config;
        List<IInstructionAnalyzer> m_Analyzers;
        List<OpCode> m_OpCodes;
        List<ProblemDescriptor> m_ProblemDescriptors;

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
            if (m_Config != null)
                throw new Exception("Module is already initialized.");

            m_Config = config;
            m_Analyzers = new List<IInstructionAnalyzer>();
            m_OpCodes = new List<OpCode>();
            m_ProblemDescriptors = new List<ProblemDescriptor>();

            foreach (var type in TypeCache.GetTypesDerivedFrom(typeof(IInstructionAnalyzer)))
                AddAnalyzer(Activator.CreateInstance(type) as IInstructionAnalyzer);
        }

        public override Task<IReadOnlyCollection<ProjectIssue>> AuditAsync(ProjectAuditorParams projectAuditorParams, IProgress progress = null)
        {
            if (m_ProblemDescriptors == null)
                throw new Exception("Descriptors Database not initialized.");

            var foundIssues = new List<ProjectIssue>();
            var compilationPipeline = new AssemblyCompilation
            {
                AssemblyCompilationFinished = (compilationTask, compilerMessages) => ProcessCompilerMessages(compilationTask, compilerMessages, foundIssues.Add),
                CompilationMode = m_Config.CompilationMode,
                Platform = projectAuditorParams.platform
            };

            Profiler.BeginSample("CodeModule.Audit.Compilation");
            var assemblyInfos = compilationPipeline.Compile(progress);
            Profiler.EndSample();

            if (m_Config.CompilationMode == CompilationMode.Editor)
            {
                foundIssues.AddRange(assemblyInfos.Select(assemblyInfo => new ProjectIssue(assemblyInfo.name, IssueCategory.Assembly,
                    new object[(int)AssemblyProperty.Num]
                    {
                        assemblyInfo.packageReadOnly,
                        "N/A"
                    })
                    {
                        location = new Location(assemblyInfo.asmDefPath)
                    }));
            }

            var assemblyDirectories = new List<string>();

            assemblyDirectories.AddRange(AssemblyInfoProvider.GetPrecompiledAssemblyDirectories(PrecompiledAssemblyTypes.UserAssembly | PrecompiledAssemblyTypes.UnityEngine));
            if (m_Config.CompilationMode == CompilationMode.Editor)
                assemblyDirectories.AddRange(AssemblyInfoProvider.GetPrecompiledAssemblyDirectories(PrecompiledAssemblyTypes.UnityEditor));
            assemblyDirectories.AddRange(assemblyInfos.Select(info => Path.GetDirectoryName(info.path)).Distinct());

            // analyze compiled assemblies
            var callCrawler = new CallCrawler();
            var analysisTask = AnalyzeAssemblies(assemblyInfos, assemblyDirectories, progress).ContinueWith(t =>
            {
                foundIssues.AddRange(t.Result.issues);
                callCrawler.AddCalls(t.Result.callInfos);

                // cleanup assemblies
                compilationPipeline.Dispose();
            });

            // final step: build call hierarchy. Note that this task will run AFTER the analysis finished.
            var buildHierarchyTask = analysisTask.ContinueWith((t) =>
            {
                var diagnostics = foundIssues.Where(i => i.category == IssueCategory.Code).ToList();
                Profiler.BeginSample("CodeModule.Audit.BuildCallHierarchies");
                callCrawler.BuildCallHierarchies(diagnostics, progress);
                Profiler.EndSample();
            });

            if (!m_Config.AnalyzeInBackground)
            {
                return buildHierarchyTask.ContinueWith<IReadOnlyCollection<ProjectIssue>>(t => foundIssues);
            }

            return analysisTask.ContinueWith<IReadOnlyCollection<ProjectIssue>>(t => foundIssues);
        }

        Task<AssemblyAnalysisResult> AnalyzeAssemblies(IReadOnlyCollection<AssemblyInfo> assemblyInfos, IReadOnlyCollection<string> assemblyDirectories, IProgress progress = null)
        {
            Profiler.BeginSample("AnalyzeAssemblies");

            var tasks = new List<Task<AssemblyAnalysisResult>>();
            foreach (var assemblyInfo in assemblyInfos)
            {
                if (!File.Exists(assemblyInfo.path))
                {
                    Debug.LogError(assemblyInfo.path + " not found.");
                    continue;
                }

                tasks.Add(Task.Run(() =>
                {
                    Profiler.BeginSample("AnalyzeAssemblies Task");

                    var taskResult = new AssemblyAnalysisResult();
                    using (var assemblyResolver = new DefaultAssemblyResolver())
                    {
                        foreach (var dir in assemblyDirectories)
                            assemblyResolver.AddSearchDirectory(dir);

                        AnalyzeAssembly(assemblyInfo, assemblyResolver, info => taskResult.callInfos.Add(info), issue => taskResult.issues.Add(issue));
                    }

                    Profiler.EndSample();

                    return taskResult;
                }));
            }

            Profiler.EndSample();

            return Task.WhenAll(tasks).ContinueWith(t =>
            {
                Profiler.BeginSample("AnalyzeAssemblies Combine Results");

                var result = new AssemblyAnalysisResult();
                foreach (var taskResult in t.Result)
                {
                    result.callInfos.AddRange(taskResult.callInfos);
                    result.issues.AddRange(taskResult.issues);
                }

                Profiler.EndSample();
                return result;
            });
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

            Profiler.BeginSample("CodeModule.IsPerformanceCriticalContext");
            var perfCriticalContext = IsPerformanceCriticalContext(caller);
            Profiler.EndSample();

            var callerNode = new CallTreeNode(caller)
            {
                perfCriticalContext = perfCriticalContext
            };

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
                            projectIssue.dependencies = callerNode; // set root
                            projectIssue.location = location;
                            projectIssue.SetCustomProperties(new object[(int)CodeProperty.Num] {assemblyInfo.name});

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

        void ProcessCompilerMessages(AssemblyCompilationTask compilationTask, CompilerMessage[] compilerMessages, Action<ProjectIssue> onIssueFound)
        {
            Profiler.BeginSample("CodeModule.ProcessCompilerMessages");

            var severity = Rule.Severity.None;
            if (compilationTask.status == CompilationStatus.MissingDependency)
                severity = Rule.Severity.Warning;
            else if (compilerMessages.Any(m => m.type == CompilerMessageType.Error))
                severity = Rule.Severity.Error;

            var assemblyInfo = AssemblyInfoProvider.GetAssemblyInfoFromAssemblyPath(compilationTask.assemblyPath);

            onIssueFound(new ProjectIssue(assemblyInfo.name, IssueCategory.Assembly,
                new object[(int)AssemblyProperty.Num]
                {
                    assemblyInfo.packageReadOnly,
                    compilationTask.durationInMs
                })
                {
                    dependencies = new AssemblyDependencyNode(assemblyInfo.name, compilationTask.dependencies.Select(d => d.assemblyName).ToArray()),
                    location = new Location(assemblyInfo.asmDefPath),
                    severity = severity
                });

            foreach (var message in compilerMessages)
            {
                if (message.code == null)
                {
                    Debug.LogWarningFormat("Missing information in compiler message for {0} assembly", assemblyInfo.name);
                    continue;
                }

                var relativePath = AssemblyInfoProvider.ResolveAssetPath(assemblyInfo, message.file);
                var issue = new ProjectIssue(message.message,
                    IssueCategory.CodeCompilerMessage,
                    new object[(int)CompilerMessageProperty.Num]
                    {
                        message.code,
                        assemblyInfo.name
                    })
                {
                    location = new Location(relativePath, message.line),
                    severity = CompilerMessageTypeToSeverity(message.type)
                };
                onIssueFound(issue);
            }

            Profiler.EndSample();
        }

        static Rule.Severity CompilerMessageTypeToSeverity(CompilerMessageType compilerMessageType)
        {
            switch (compilerMessageType)
            {
                case CompilerMessageType.Error:
                    return Rule.Severity.Error;
                case CompilerMessageType.Warning:
                    return Rule.Severity.Warning;
                case CompilerMessageType.Info:
                    return Rule.Severity.Info;
            }

            return Rule.Severity.Info;
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
