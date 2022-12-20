using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.ProjectAuditor.Editor.AssemblyUtils;
using Unity.ProjectAuditor.Editor.CodeAnalysis;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using Object = System.Object;
using PropertyDefinition = Unity.ProjectAuditor.Editor.Core.PropertyDefinition;
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
                new PropertyDefinition { type = PropertyType.LogLevel},
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
                new PropertyDefinition { type = PropertyType.Severity, format = PropertyFormat.String, name = "Severity"},
                new PropertyDefinition { type = PropertyType.Area, name = "Area", longName = "The area the issue might have an impact on"},
                new PropertyDefinition { type = PropertyType.Filename, name = "Filename", longName = "Filename and line number"},
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(CodeProperty.Assembly), format = PropertyFormat.String, name = "Assembly", longName = "Managed Assembly name" },
                new PropertyDefinition { type = PropertyType.Descriptor, name = "Descriptor", defaultGroup = true, hidden = true},
            }
        };

        static readonly IssueLayout k_CompilerMessageLayout = new IssueLayout
        {
            category = IssueCategory.CodeCompilerMessage,
            properties = new[]
            {
                new PropertyDefinition { type = PropertyType.LogLevel, name = "Log Level"},
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
        List<ICodeModuleInstructionAnalyzer> m_Analyzers;
        List<OpCode> m_OpCodes;
        HashSet<Descriptor> m_Descriptors;

        Thread m_AssemblyAnalysisThread;

        public override string name => "Code";

        public override IReadOnlyCollection<Descriptor> supportedDescriptors => m_Descriptors;

        public override IReadOnlyCollection<IssueLayout> supportedLayouts => new IssueLayout[]
        {
            k_AssemblyLayout,
            k_PrecompiledAssemblyLayout,
            k_IssueLayout,
            k_CompilerMessageLayout,
            k_GenericIssueLayout,
        };

        public override void Initialize(ProjectAuditorConfig config)
        {
            if (m_Config != null)
                throw new Exception("Module is already initialized.");

            m_Config = config;
            m_Analyzers = new List<ICodeModuleInstructionAnalyzer>();
            m_OpCodes = new List<OpCode>();
            m_Descriptors = new HashSet<Descriptor>();

            foreach (var type in TypeCache.GetTypesDerivedFrom(typeof(ICodeModuleInstructionAnalyzer)))
                AddAnalyzer(Activator.CreateInstance(type) as ICodeModuleInstructionAnalyzer);
        }

        public override void Audit(ProjectAuditorParams projectAuditorParams, IProgress progress = null)
        {
            if (m_Descriptors == null)
                throw new Exception("Descriptors Database not initialized.");

            if (m_Config.AnalyzeInBackground && m_AssemblyAnalysisThread != null)
                m_AssemblyAnalysisThread.Join();

            var precompiledAssemblies = AssemblyInfoProvider.GetPrecompiledAssemblyPaths(PrecompiledAssemblyTypes.All)
                .Select(assemblyPath => (ProjectIssue)ProjectIssue
                    .Create(IssueCategory.PrecompiledAssembly, Path.GetFileNameWithoutExtension(assemblyPath))
                    .WithCustomProperties(new object[(int)PrecompiledAssemblyProperty.Num]
                    {
                        false
                    })
                    .WithLocation(assemblyPath))
                .ToArray();
            if (precompiledAssemblies.Any())
                projectAuditorParams.onIncomingIssues(precompiledAssemblies);

            var roslynAnalyzerAssets = AssetDatabase.FindAssets("l:RoslynAnalyzer").Select(AssetDatabase.GUIDToAssetPath)
                .ToArray();
            var roslynAnalyzerIssues = roslynAnalyzerAssets
                .Select(roslynAnalyzerDllPath => (ProjectIssue)ProjectIssue.Create(
                IssueCategory.PrecompiledAssembly,
                Path.GetFileNameWithoutExtension(roslynAnalyzerDllPath))
                .WithCustomProperties(new object[(int)PrecompiledAssemblyProperty.Num]
                {
                    true
                })
                .WithLocation(roslynAnalyzerDllPath));

            projectAuditorParams.onIncomingIssues(roslynAnalyzerIssues);

            var compilationPipeline = new AssemblyCompilation
            {
                onAssemblyCompilationFinished = (compilationTask, compilerMessages) =>
                {
                    projectAuditorParams.onIncomingIssues(ProcessCompilerMessages(compilationTask, compilerMessages));
                },
                codeOptimization = projectAuditorParams.codeOptimization,
                compilationMode = m_Config.CompilationMode,
                platform = projectAuditorParams.platform,
                roslynAnalyzers = m_Config.UseRoslynAnalyzers ? roslynAnalyzerAssets : null,
                assemblyNames = projectAuditorParams.assemblyNames
            };

            Profiler.BeginSample("CodeModule.Audit.Compilation");
            var assemblyInfos = compilationPipeline.Compile(progress);
            Profiler.EndSample();

            if (projectAuditorParams.assemblyNames != null)
            {
                assemblyInfos = assemblyInfos.Where(a => projectAuditorParams.assemblyNames.Contains(a.name)).ToArray();
            }

            if (m_Config.CompilationMode == CompilationMode.Editor)
            {
                var issues = assemblyInfos.Select(assemblyInfo => (ProjectIssue)ProjectIssue
                    .Create(IssueCategory.Assembly, assemblyInfo.name)
                    .WithCustomProperties(new object[(int)AssemblyProperty.Num]
                    {
                        assemblyInfo.packageReadOnly,
                        float.NaN
                    })
                    .WithLocation(assemblyInfo.asmDefPath))
                    .ToArray();
                if (issues.Any())
                    projectAuditorParams.onIncomingIssues(issues);
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
                // remove issues if platform does not match
                var platformString = projectAuditorParams.platform.ToString();
                foundIssues.RemoveAll(i => i.descriptor != null && i.descriptor.platforms != null && i.descriptor.platforms.Length > 0 && !i.descriptor.platforms.Contains(platformString));

                var diagnostics = foundIssues.Where(i => i.category != IssueCategory.GenericInstance).ToList();
                Profiler.BeginSample("CodeModule.Audit.BuildCallHierarchies");
                compilationPipeline.Dispose();
                callCrawler.BuildCallHierarchies(diagnostics, bar);
                Profiler.EndSample();

                foreach (var d in diagnostics)
                {
                    // upgrade to major severity if issue is found in a hot-path
                    if (!d.IsMajorOrCritical() && d.dependencies != null && d.dependencies.perfCriticalContext)
                        d.severity = Severity.Major;
                }

                // workaround for empty 'relativePath' strings which are not all available when 'onIssueFoundInternal' is called
                if (foundIssues.Any())
                    projectAuditorParams.onIncomingIssues(foundIssues);
                projectAuditorParams.onModuleCompleted?.Invoke();
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

                    AnalyzeAssembly(assemblyInfo, assemblyResolver, onCallFound, assemblyFilters == null || assemblyFilters.Contains(assemblyInfo.name) ? onIssueFound : null);
                }
            }

            progress?.Clear();
            onComplete?.Invoke(progress);
        }

        public override void RegisterDescriptor(Descriptor descriptor)
        {
            if (!m_Descriptors.Add(descriptor))
                throw new Exception("Duplicate descriptor with id: " + descriptor.id);
        }

        void AnalyzeAssembly(AssemblyInfo assemblyInfo, IAssemblyResolver assemblyResolver, Action<CallInfo> onCallFound, Action<ProjectIssue> onIssueFound)
        {
            Profiler.BeginSample("CodeModule.Analyze " + assemblyInfo.name);

            using (var assembly = AssemblyDefinition.ReadAssembly(assemblyInfo.path,
                new ReaderParameters {ReadSymbols = true, AssemblyResolver = assemblyResolver, MetadataResolver = new MetadataResolverWithCache(assemblyResolver)}))
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
                    if (analyzer.opCodes.Contains(inst.OpCode))
                    {
                        Profiler.BeginSample("CodeModule " + analyzer.GetType().Name);
                        var issueBuilder = analyzer.Analyze(caller, inst);
                        if (issueBuilder != null)
                        {
                            issueBuilder.WithDependencies(callerNode); // set root
                            issueBuilder.WithLocation(location);
                            issueBuilder.WithCustomProperties(new object[(int)CodeProperty.Num] {assemblyInfo.name});

                            onIssueFound(issueBuilder);
                        }
                        Profiler.EndSample();
                    }
                Profiler.EndSample();
            }
            Profiler.EndSample();
        }

        void AddAnalyzer(ICodeModuleInstructionAnalyzer analyzer)
        {
            analyzer.Initialize(this);
            m_Analyzers.Add(analyzer);
            m_OpCodes.AddRange(analyzer.opCodes);
        }

        IEnumerable<ProjectIssue> ProcessCompilerMessages(AssemblyCompilationTask compilationTask, CompilerMessage[] compilerMessages)
        {
            Profiler.BeginSample("CodeModule.ProcessCompilerMessages");

            var severity = Severity.None;
            if (compilationTask.status == CompilationStatus.MissingDependency)
                severity = Severity.Warning;
            else if (compilerMessages.Any(m => m.type == CompilerMessageType.Error))
                severity = Severity.Error;

            var assemblyInfo = AssemblyInfoProvider.GetAssemblyInfoFromAssemblyPath(compilationTask.assemblyPath);
            yield return ProjectIssue.Create(IssueCategory.Assembly, assemblyInfo.name)
                .WithCustomProperties(new object[(int)AssemblyProperty.Num]
                {
                    assemblyInfo.packageReadOnly,
                    compilationTask.durationInMs
                })
                .WithDependencies(new AssemblyDependencyNode(assemblyInfo.name,
                    compilationTask.dependencies.Select(d => d.assemblyName).ToArray()))
                .WithLocation(assemblyInfo.asmDefPath)
                .WithSeverity(severity);

            foreach (var message in compilerMessages)
            {
                if (message.code == null)
                {
                    Debug.LogWarningFormat("Missing information in compiler message for {0} assembly", assemblyInfo.name);
                    continue;
                }

                var relativePath = AssemblyInfoProvider.ResolveAssetPath(assemblyInfo, message.file);
                yield return ProjectIssue.Create(IssueCategory.CodeCompilerMessage, message.message)
                    .WithCustomProperties(new object[(int)CompilerMessageProperty.Num]
                    {
                        message.code,
                        assemblyInfo.name
                    })
                    .WithLocation(relativePath, message.line)
                    .WithLogLevel(CompilerMessageTypeToLogLevel(message.type));
            }

            Profiler.EndSample();
        }

        static LogLevel CompilerMessageTypeToLogLevel(CompilerMessageType compilerMessageType)
        {
            switch (compilerMessageType)
            {
                case CompilerMessageType.Error:
                    return LogLevel.Error;
                case CompilerMessageType.Warning:
                    return LogLevel.Warning;
                case CompilerMessageType.Info:
                    return LogLevel.Info;
            }

            return LogLevel.Info;
        }

        static bool IsPerformanceCriticalContext(MethodDefinition methodDefinition)
        {
            if (MonoBehaviourAnalysis.IsMonoBehaviour(methodDefinition.DeclaringType) &&
                MonoBehaviourAnalysis.IsMonoBehaviourUpdateMethod(methodDefinition))
                return true;
#if PACKAGE_ENTITIES
            if (ComponentSystemAnalysis.IsComponentSystem(methodDefinition.DeclaringType) &&
                ComponentSystemAnalysis.IsOnUpdateMethod(methodDefinition))
                return true;
#endif
            return false;
        }
    }
}
