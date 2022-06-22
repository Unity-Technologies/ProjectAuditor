using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
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

    public enum PrecompiledAssemblyProperty
    {
        RoslynAnalyzer = 0,
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
        static readonly IssueLayout k_AssemblyLayout = new IssueLayout
        {
            category = IssueCategory.Assembly,
            properties = new[]
            {
                new PropertyDefinition { type = PropertyType.Severity},
                new PropertyDefinition { type = PropertyType.Description, name = "Assembly Name"},
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AssemblyProperty.CompileTime), format = PropertyFormat.Time, name = "Compile Time (seconds)"},
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AssemblyProperty.ReadOnly), format = PropertyFormat.Bool, name = "Read Only", defaultGroup = true},
                new PropertyDefinition { type = PropertyType.Path, name = "asmdef Path"},
            }
        };

        static readonly IssueLayout k_PrecompiledAssemblyLayout = new IssueLayout
        {
            category = IssueCategory.PrecompiledAssembly,
            properties = new[]
            {
                new PropertyDefinition { type = PropertyType.Description, name = "Assembly Name"},
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(PrecompiledAssemblyProperty.RoslynAnalyzer), format = PropertyFormat.Bool, name = "Roslyn Analyzer"},
                new PropertyDefinition { type = PropertyType.Directory, name = "Path", defaultGroup = true},
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
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(CodeProperty.Assembly), format = PropertyFormat.String, name = "Assembly", longName = "Managed Assembly name" },
                new PropertyDefinition { type = PropertyType.Descriptor, name = "Descriptor", defaultGroup = true},
            }
        };

        static readonly IssueLayout k_CompilerMessageLayout = new IssueLayout
        {
            category = IssueCategory.CodeCompilerMessage,
            properties = new[]
            {
                new PropertyDefinition { type = PropertyType.Severity, name = "Type"},
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(CompilerMessageProperty.Code), format = PropertyFormat.String, name = "Code", defaultGroup = true},
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

        Thread m_AssemblyAnalysisThread;

        public override IEnumerable<ProblemDescriptor> GetDescriptors()
        {
            return m_ProblemDescriptors;
        }

        public override IEnumerable<IssueLayout> GetLayouts()
        {
            yield return k_AssemblyLayout;
            yield return k_PrecompiledAssemblyLayout;
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

        public override void Audit(ProjectAuditorParams projectAuditorParams, IProgress progress = null)
        {
            if (m_ProblemDescriptors == null)
                throw new Exception("Descriptors Database not initialized.");

            if (m_Config.AnalyzeInBackground && m_AssemblyAnalysisThread != null)
                m_AssemblyAnalysisThread.Join();

            foreach (var assemblyPath in AssemblyInfoProvider.GetPrecompiledAssemblyPaths(PrecompiledAssemblyTypes.All))
            {
                projectAuditorParams.onIssueFound(new ProjectIssue(Path.GetFileNameWithoutExtension(assemblyPath), IssueCategory.PrecompiledAssembly,
                    new object[(int)PrecompiledAssemblyProperty.Num]
                    {
                        false
                    })
                    {
                        location = new Location(assemblyPath)
                    });
            }

            var roslynAnalyzers = new string[] {};
            if (m_Config.UseRoslynAnalyzers)
            {
                roslynAnalyzers = AssetDatabase.FindAssets("l:RoslynAnalyzer").Select(AssetDatabase.GUIDToAssetPath)
                    .ToArray();
                foreach (var assemblyPath in roslynAnalyzers)
                {
                    projectAuditorParams.onIssueFound(new ProjectIssue(Path.GetFileNameWithoutExtension(assemblyPath), IssueCategory.PrecompiledAssembly,
                        new object[(int)PrecompiledAssemblyProperty.Num]
                        {
                            true
                        })
                        {
                            location = new Location(assemblyPath)
                        });
                }
            }

            if (m_Config.UseRoslynAnalyzers)
            {
                roslynAnalyzers = AssetDatabase.FindAssets("l:RoslynAnalyzer").Select(AssetDatabase.GUIDToAssetPath)
                    .ToArray();
            }

            var compilationPipeline = new AssemblyCompilation
            {
                onAssemblyCompilationFinished = (compilationTask, compilerMessages) => ProcessCompilerMessages(compilationTask, compilerMessages, projectAuditorParams.onIssueFound),
                codeOptimization = projectAuditorParams.codeOptimization,
                compilationMode = m_Config.CompilationMode,
                platform = projectAuditorParams.platform,
                roslynAnalyzers = roslynAnalyzers,
                assemblyNames = projectAuditorParams.assemblyNames
            };

            Profiler.BeginSample("CodeModule.Audit.Compilation");
            var assemblyInfos = compilationPipeline.Compile(progress);
            Profiler.EndSample();

            if (m_Config.CompilationMode == CompilationMode.Editor)
            {
                foreach (var assemblyInfo in assemblyInfos)
                {
                    projectAuditorParams.onIssueFound(new ProjectIssue(assemblyInfo.name, IssueCategory.Assembly,
                        new object[(int)AssemblyProperty.Num]
                        {
                            assemblyInfo.packageReadOnly,
                            "N/A"
                        })
                        {
                            location = new Location(assemblyInfo.asmDefPath)
                        });
                }
            }

            // process successfully compiled assemblies
            var localAssemblyInfos = assemblyInfos.Where(info => !info.packageReadOnly).ToArray();
            var readOnlyAssemblyInfos = assemblyInfos.Where(info => info.packageReadOnly).ToArray();
            var foundIssues = new List<ProjectIssue>();
            var callCrawler = new CallCrawler();
            var onCallFound = new Action<CallInfo>(pair =>
            {
                callCrawler.Add(pair);
            });
            var onIssueFoundInternal = new Action<ProjectIssue>(foundIssues.Add);
            var onCompleteInternal = new Action<IProgress>(bar =>
            {
                var diagnostics = foundIssues.Where(i => i.category != IssueCategory.GenericInstance).ToList();
                Profiler.BeginSample("CodeModule.Audit.BuildCallHierarchies");
                compilationPipeline.Dispose();
                callCrawler.BuildCallHierarchies(diagnostics, bar);
                Profiler.EndSample();

                // workaround for empty 'relativePath' strings which are not all available when 'onIssueFoundInternal' is called
                foreach (var issue in foundIssues)
                    projectAuditorParams.onIssueFound(issue);

                if (projectAuditorParams.onComplete != null)
                    projectAuditorParams.onComplete();
            });

            var assemblyDirectories = new List<string>();

            assemblyDirectories.AddRange(AssemblyInfoProvider.GetPrecompiledAssemblyDirectories(PrecompiledAssemblyTypes.UserAssembly | PrecompiledAssemblyTypes.UnityEngine | PrecompiledAssemblyTypes.SystemAssembly));
            if (m_Config.CompilationMode == CompilationMode.Editor)
                assemblyDirectories.AddRange(AssemblyInfoProvider.GetPrecompiledAssemblyDirectories(PrecompiledAssemblyTypes.UnityEditor));

            Profiler.BeginSample("CodeModule.Audit.Analysis");

            // first phase: analyze assemblies generated from editable scripts
            AnalyzeAssemblies(localAssemblyInfos, projectAuditorParams.assemblyNames, assemblyDirectories, onCallFound, onIssueFoundInternal, null, progress);

            var enableBackgroundAnalysis = m_Config.AnalyzeInBackground;
#if !UNITY_2019_3_OR_NEWER
            enableBackgroundAnalysis = false;
#endif
            // second phase: analyze all remaining assemblies, in a separate thread if enableBackgroundAnalysis is enabled
            if (enableBackgroundAnalysis)
            {
                m_AssemblyAnalysisThread = new Thread(() =>
                    AnalyzeAssemblies(readOnlyAssemblyInfos, projectAuditorParams.assemblyNames, assemblyDirectories, onCallFound, onIssueFoundInternal, onCompleteInternal));
                m_AssemblyAnalysisThread.Name = "Assembly Analysis";
                m_AssemblyAnalysisThread.Priority = ThreadPriority.BelowNormal;
                m_AssemblyAnalysisThread.Start();
            }
            else
            {
                Profiler.BeginSample("CodeModule.Audit.AnalysisReadOnly");
                AnalyzeAssemblies(readOnlyAssemblyInfos, projectAuditorParams.assemblyNames, assemblyDirectories, onCallFound, onIssueFoundInternal, onCompleteInternal, progress);
                Profiler.EndSample();
            }
            Profiler.EndSample();
        }

        void AnalyzeAssemblies(IReadOnlyCollection<AssemblyInfo> assemblyInfos, IReadOnlyCollection<string> assemblyFilters, IReadOnlyCollection<string> assemblyDirectories, Action<CallInfo> onCallFound, Action<ProjectIssue> onIssueFound, Action<IProgress> onComplete, IProgress progress = null)
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
                    if (progress != null)
                        progress.Advance(assemblyInfo.name);

                    if (!File.Exists(assemblyInfo.path))
                    {
                        Debug.LogError(assemblyInfo.path + " not found.");
                        continue;
                    }

                    AnalyzeAssembly(assemblyInfo, assemblyResolver, onCallFound, (assemblyFilters == null || assemblyFilters.Contains(assemblyInfo.name)) ? onIssueFound : null);
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

                // skip analyzers if we are not interested in reporting issues
                if (onIssueFound == null)
                    continue;

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
