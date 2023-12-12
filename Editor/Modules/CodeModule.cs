using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.ProjectAuditor.Editor.AssemblyUtils;
using Unity.ProjectAuditor.Editor.CodeAnalysis;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using Unity.ProjectAuditor.Editor.Interfaces;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using Object = System.Object;
using PropertyDefinition = Unity.ProjectAuditor.Editor.Core.PropertyDefinition;
using ThreadPriority = System.Threading.ThreadPriority;

namespace Unity.ProjectAuditor.Editor.Modules
{
    enum AssemblyProperty
    {
        ReadOnly = 0,
        CompileTime,
        Num
    }

    enum PrecompiledAssemblyProperty
    {
        RoslynAnalyzer = 0,
        Num
    }

    internal enum CodeProperty
    {
        Assembly = 0,
        Num
    }

    enum CompilerMessageProperty
    {
        Code = 0,
        Assembly,
        Num
    }

    class CodeModule : ModuleWithAnalyzers<ICodeModuleInstructionAnalyzer>
    {
        static readonly IssueLayout k_AssemblyLayout = new IssueLayout
        {
            Category = IssueCategory.Assembly,
            Properties = new[]
            {
                new PropertyDefinition { Type = PropertyType.LogLevel},
                new PropertyDefinition { Type = PropertyType.Description, Name = "Assembly Name", MaxAutoWidth = 800},
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(AssemblyProperty.CompileTime), Format = PropertyFormat.Time, Name = "Compile Time (seconds)"},
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(AssemblyProperty.ReadOnly), Format = PropertyFormat.Bool, Name = "Read Only", IsDefaultGroup = true},
                new PropertyDefinition { Type = PropertyType.Path, Name = "asmdef Path"},
            }
        };

        static readonly IssueLayout k_PrecompiledAssemblyLayout = new IssueLayout
        {
            Category = IssueCategory.PrecompiledAssembly,
            Properties = new[]
            {
                new PropertyDefinition { Type = PropertyType.Description, Name = "Assembly Name"},
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(PrecompiledAssemblyProperty.RoslynAnalyzer), Format = PropertyFormat.Bool, Name = "Roslyn Analyzer"},
                new PropertyDefinition { Type = PropertyType.Directory, Name = "Path", IsDefaultGroup = true},
            }
        };

        static readonly IssueLayout k_IssueLayout = new IssueLayout
        {
            Category = IssueCategory.Code,
            Properties = new[]
            {
                new PropertyDefinition { Type = PropertyType.Description, Name = "Issue", LongName = "Issue description", MaxAutoWidth = 800 },
                new PropertyDefinition { Type = PropertyType.Severity, Format = PropertyFormat.String, Name = "Severity"},
                new PropertyDefinition { Type = PropertyType.Areas, Name = "Areas", LongName = "The areas the issue might have an impact on"},
                new PropertyDefinition { Type = PropertyType.Filename, Name = "Filename", LongName = "Filename and line number"},
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(CodeProperty.Assembly), Format = PropertyFormat.String, Name = "Assembly", LongName = "Managed Assembly name" },
                new PropertyDefinition { Type = PropertyType.Descriptor, Name = "Descriptor", IsDefaultGroup = true, IsHidden = true},
            }
        };

        static readonly IssueLayout k_CompilerMessageLayout = new IssueLayout
        {
            Category = IssueCategory.CodeCompilerMessage,
            Properties = new[]
            {
                new PropertyDefinition { Type = PropertyType.LogLevel, Name = "Log Level"},
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(CompilerMessageProperty.Code), Format = PropertyFormat.String, Name = "Code", IsDefaultGroup = true},
                new PropertyDefinition { Type = PropertyType.Description, Format = PropertyFormat.String, Name = "Message", LongName = "Compiler Message"},
                new PropertyDefinition { Type = PropertyType.Filename, Name = "Filename", LongName = "Filename and line number"},
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(CompilerMessageProperty.Assembly), Format = PropertyFormat.String, Name = "Target Assembly", LongName = "Managed Assembly name" },
                new PropertyDefinition { Type = PropertyType.Path, Name = "Full path"},
            }
        };

        static readonly IssueLayout k_GenericIssueLayout = new IssueLayout
        {
            Category = IssueCategory.GenericInstance,
            Properties = new[]
            {
                new PropertyDefinition { Type = PropertyType.Description, Name = "Generic Type"},
                new PropertyDefinition { Type = PropertyType.Filename, Name = "Filename", LongName = "Filename and line number"},
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(CodeProperty.Assembly), Format = PropertyFormat.String, Name = "Assembly", LongName = "Managed Assembly name" }
            }
        };

        static readonly IssueLayout k_DomainReloadIssueLayout = new IssueLayout
        {
            Category = IssueCategory.DomainReload,
            Properties = new[]
            {
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(CompilerMessageProperty.Code), Format = PropertyFormat.String, Name = "Code", IsDefaultGroup = true},
                new PropertyDefinition { Type = PropertyType.Description, Name = "Issue", LongName = "Issue description", MaxAutoWidth = 800 },
                new PropertyDefinition { Type = PropertyType.Filename, Name = "Filename", LongName = "Filename and line number"},
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(CompilerMessageProperty.Assembly), Format = PropertyFormat.String, Name = "Assembly", LongName = "Managed Assembly name" },
                new PropertyDefinition { Type = PropertyType.Descriptor, Name = "Descriptor", IsDefaultGroup = true, IsHidden = true},
            }
        };

        List<OpCode> m_OpCodes;

        Thread m_AssemblyAnalysisThread;

        public override string Name => "Code";

        // Match a whole "word", starting with UDR and ending with exactly 4 digits, e.g. UDR1234
        static readonly Regex s_RegEx = new Regex(@"\bUDR\d{4}\b");

        public override IReadOnlyCollection<IssueLayout> SupportedLayouts => new IssueLayout[]
        {
            k_IssueLayout,
            k_AssemblyLayout,
            k_PrecompiledAssemblyLayout,
            k_CompilerMessageLayout,
            k_GenericIssueLayout,
            k_DomainReloadIssueLayout
        };

        public override void Initialize()
        {
            base.Initialize();

            m_OpCodes = m_Analyzers.Select(a => a.opCodes).SelectMany(c => c).Distinct().ToList();
        }

        public override AnalysisResult Audit(AnalysisParams analysisParams, IProgress progress = null)
        {
            if (m_Ids == null)
                throw new Exception("Descriptors Database not initialized.");

            if (UserPreferences.AnalyzeInBackground && m_AssemblyAnalysisThread != null)
                m_AssemblyAnalysisThread.Join();

            var context = new AnalysisContext()
            {
                Params = analysisParams
            };

            var precompiledAssemblies = AssemblyInfoProvider.GetPrecompiledAssemblyPaths(PrecompiledAssemblyTypes.All)
                .Select(assemblyPath => (ProjectIssue)context.CreateInsight(IssueCategory.PrecompiledAssembly, Path.GetFileNameWithoutExtension(assemblyPath))
                    .WithCustomProperties(new object[(int)PrecompiledAssemblyProperty.Num]
                    {
                        false
                    })
                    .WithLocation(assemblyPath))
                .ToArray();
            if (precompiledAssemblies.Any())
                analysisParams.OnIncomingIssues(precompiledAssemblies);

            // find all roslyn analyzer DLLs by label
            var roslynAnalyzerAssets = AssetDatabase.FindAssets("l:RoslynAnalyzer").Select(AssetDatabase.GUIDToAssetPath).ToList();

            // find all roslyn analyzers packaged with Project Auditor
            if (Directory.Exists($"{ProjectAuditor.PackagePath}/RoslynAnalyzers"))
            {
                var assetPaths = AssetDatabase.FindAssets("", new[] { $"{ProjectAuditor.PackagePath}/RoslynAnalyzers" }).Select(AssetDatabase.GUIDToAssetPath);
                foreach (var assetPath in assetPaths)
                {
                    if (assetPath.EndsWith(".dll"))
                        roslynAnalyzerAssets.Add(assetPath);
                }
            }

            // report all roslyn analyzers as PrecompiledAssembly issues
            var roslynAnalyzerIssues = roslynAnalyzerAssets
                .Select(roslynAnalyzerDllPath => (ProjectIssue)context.CreateInsight(
                IssueCategory.PrecompiledAssembly,
                Path.GetFileNameWithoutExtension(roslynAnalyzerDllPath))
                .WithCustomProperties(new object[(int)PrecompiledAssemblyProperty.Num]
                {
                    true
                })
                .WithLocation(roslynAnalyzerDllPath));

            analysisParams.OnIncomingIssues(roslynAnalyzerIssues);

            var assemblyDirectories = new List<string>();
            var compilationPipeline = new AssemblyCompilation
            {
                OnAssemblyCompilationFinished = (compilationTask, compilerMessages) =>
                {
                    analysisParams.OnIncomingIssues(ProcessCompilerMessages(context, compilationTask, compilerMessages));
                },
                CodeOptimization = analysisParams.CodeOptimization,
                CompilationMode = analysisParams.CompilationMode,
                Platform = analysisParams.Platform,
                // TODO: reminder to add list of analyzers to metadata
                RoslynAnalyzers = UserPreferences.UseRoslynAnalyzers ? roslynAnalyzerAssets.ToArray() : null,
                AssemblyNames = analysisParams.AssemblyNames
            };

            Profiler.BeginSample("CodeModule.Audit.Compilation");
            var assemblyInfos = compilationPipeline.Compile(progress);
            Profiler.EndSample();

            if (progress?.IsCancelled ?? false)
                return AnalysisResult.Cancelled;

            if (analysisParams.AssemblyNames != null)
            {
                assemblyInfos = assemblyInfos.Where(a => analysisParams.AssemblyNames.Contains(a.Name)).ToArray();
            }

            if (analysisParams.CompilationMode == CompilationMode.Editor ||
                analysisParams.CompilationMode == CompilationMode.EditorPlayMode)
            {
                var issues = assemblyInfos.Select(assemblyInfo => (ProjectIssue)context.CreateInsight(IssueCategory.Assembly, assemblyInfo.Name)
                    .WithCustomProperties(new object[(int)AssemblyProperty.Num]
                    {
                        assemblyInfo.IsPackageReadOnly,
                        float.NaN
                    })
                    .WithLocation(assemblyInfo.AsmDefPath))
                    .ToArray();
                if (issues.Any())
                    analysisParams.OnIncomingIssues(issues);
            }

            // process successfully compiled assemblies
            var localAssemblyInfos = assemblyInfos.Where(info => !info.IsPackageReadOnly).ToArray();
            var readOnlyAssemblyInfos = assemblyInfos.Where(info => info.IsPackageReadOnly).ToArray();
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
                foundIssues.RemoveAll(i => i.Id.IsValid() &&
                    !i.Id.GetDescriptor().IsApplicable(analysisParams));

                var diagnostics = foundIssues.Where(i => i.Category != IssueCategory.GenericInstance).ToList();
                Profiler.BeginSample("CodeModule.Audit.BuildCallHierarchies");
                compilationPipeline.Dispose();
                callCrawler.BuildCallHierarchies(diagnostics, bar);
                Profiler.EndSample();

                foreach (var d in diagnostics)
                {
                    // bump severity if issue is found in a hot-path
                    if (!d.IsMajorOrCritical() && d.Dependencies != null && d.Dependencies.PerfCriticalContext)
                    {
                        switch (d.Severity)
                        {
                            case Severity.Minor:
                                d.Severity = Severity.Moderate;
                                break;
                            case Severity.Moderate:
                                d.Severity = Severity.Major;
                                break;
                        }
                    }
                }

                // workaround for empty 'relativePath' strings which are not all available when 'onIssueFoundInternal' is called
                if (foundIssues.Any())
                    analysisParams.OnIncomingIssues(foundIssues);
                analysisParams.OnModuleCompleted?.Invoke(AnalysisResult.Success);
            });

            assemblyDirectories.AddRange(AssemblyInfoProvider.GetPrecompiledAssemblyDirectories(PrecompiledAssemblyTypes.UserAssembly | PrecompiledAssemblyTypes.UnityEngine | PrecompiledAssemblyTypes.SystemAssembly));
            if (analysisParams.CompilationMode == CompilationMode.Editor)
                assemblyDirectories.AddRange(AssemblyInfoProvider.GetPrecompiledAssemblyDirectories(PrecompiledAssemblyTypes.UnityEditor));

            Profiler.BeginSample("CodeModule.Audit.Analysis");

            // first phase: analyze assemblies generated from editable scripts
            AnalyzeAssemblies(localAssemblyInfos, analysisParams.AssemblyNames, assemblyDirectories, onCallFound, onIssueFoundInternal, null, progress);
            if (progress?.IsCancelled ?? false)
                return AnalysisResult.Cancelled;

            var enableBackgroundAnalysis = UserPreferences.AnalyzeInBackground;

            // second phase: analyze all remaining assemblies, in a separate thread if enableBackgroundAnalysis is enabled
            if (enableBackgroundAnalysis)
            {
                m_AssemblyAnalysisThread = new Thread(() =>
                    AnalyzeAssemblies(readOnlyAssemblyInfos, analysisParams.AssemblyNames, assemblyDirectories, onCallFound, onIssueFoundInternal, onCompleteInternal));
                m_AssemblyAnalysisThread.Name = "Assembly Analysis";
                m_AssemblyAnalysisThread.Priority = ThreadPriority.BelowNormal;
                m_AssemblyAnalysisThread.Start();
            }
            else
            {
                Profiler.BeginSample("CodeModule.Audit.AnalysisReadOnly");
                AnalyzeAssemblies(readOnlyAssemblyInfos, analysisParams.AssemblyNames, assemblyDirectories, onCallFound, onIssueFoundInternal, onCompleteInternal, progress);
                Profiler.EndSample();
            }
            Profiler.EndSample();

            if (progress?.IsCancelled ?? false)
                return AnalysisResult.Cancelled;
            return AnalysisResult.InProgress;
        }

        void AnalyzeAssemblies(IReadOnlyCollection<AssemblyInfo> assemblyInfos, IReadOnlyCollection<string> assemblyFilters, IReadOnlyCollection<string> assemblyDirectories, Action<CallInfo> onCallFound, Action<ProjectIssue> onIssueFound, Action<IProgress> onComplete, IProgress progress = null)
        {
            using (var assemblyResolver = new DefaultAssemblyResolver())
            {
                foreach (var path in assemblyDirectories)
                    assemblyResolver.AddSearchDirectory(path);

                foreach (var dir in assemblyInfos.Select(info => Path.GetDirectoryName(info.Path)).Distinct())
                    assemblyResolver.AddSearchDirectory(dir);

                if (progress != null)
                    progress.Start("Analyzing Assemblies", string.Empty, assemblyInfos.Count());

                // Analyse all Player assemblies
                foreach (var assemblyInfo in assemblyInfos)
                {
                    if (progress?.IsCancelled ?? false)
                        return;

                    if (progress != null)
                        progress.Advance(assemblyInfo.Name);

                    if (!File.Exists(assemblyInfo.Path))
                    {
                        Debug.LogError(assemblyInfo.Path + " not found.");
                        continue;
                    }

                    AnalyzeAssembly(assemblyInfo, assemblyResolver, onCallFound, assemblyFilters == null || assemblyFilters.Contains(assemblyInfo.Name) ? onIssueFound : null);
                }
            }

            progress?.Clear();
            onComplete?.Invoke(progress);
        }

        void AnalyzeAssembly(AssemblyInfo assemblyInfo, IAssemblyResolver assemblyResolver, Action<CallInfo> onCallFound, Action<ProjectIssue> onIssueFound)
        {
            Profiler.BeginSample("CodeModule.Analyze " + assemblyInfo.Name);

            using (var assembly = AssemblyDefinition.ReadAssembly(assemblyInfo.Path,
                new ReaderParameters {ReadSymbols = true, AssemblyResolver = assemblyResolver, MetadataResolver = new MetadataResolverWithCache(assemblyResolver)}))
            {
                foreach (var methodDefinition in MonoCecilHelper.AggregateAllTypeDefinitions(assembly.MainModule.Types)
                         .SelectMany(t => t.Methods))
                {
                    if (!methodDefinition.HasBody)
                        continue;

                    // workaround for long analysis times when Burst is installed
                    if (methodDefinition.DeclaringType.FullName.StartsWith("Unity.Burst.Editor.BurstDisassembler"))
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
                PerfCriticalContext = perfCriticalContext
            };

            var instructions = caller.Body.Instructions.Where(i => m_OpCodes.Contains(i.OpCode));
            foreach (var inst in instructions)
            {
                SequencePoint s = null;
                for (var i = inst; i != null; i = i.Previous)
                {
                    s = caller.DebugInformation.GetSequencePoint(i);
                    if (s != null)
                        break;
                }

                Location location = null;
                if (s != null)
                {
                    location = new Location(AssemblyInfoProvider.ResolveAssetPath(assemblyInfo, s.Document.Url), s.IsHidden ? 0 : s.StartLine);
                    callerNode.Location = location;
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

                var context = new InstructionAnalysisContext
                {
                    Instruction = inst,
                    MethodDefinition = caller
                };

                Profiler.BeginSample("CodeModule.Analyzing " + inst.OpCode.Name);
                foreach (var analyzer in m_Analyzers)
                    if (analyzer.opCodes.Contains(inst.OpCode))
                    {
                        Profiler.BeginSample("CodeModule " + analyzer.GetType().Name);
                        var issueBuilder = analyzer.Analyze(context);
                        if (issueBuilder != null)
                        {
                            issueBuilder.WithDependencies(callerNode); // set root
                            issueBuilder.WithLocation(location);
                            issueBuilder.WithCustomProperties(new object[(int)CodeProperty.Num] {assemblyInfo.Name});

                            onIssueFound(issueBuilder);
                        }
                        Profiler.EndSample();
                    }
                Profiler.EndSample();
            }
            Profiler.EndSample();
        }

        IEnumerable<ProjectIssue> ProcessCompilerMessages(AnalysisContext context, AssemblyCompilationTask compilationTask, CompilerMessage[] compilerMessages)
        {
            Profiler.BeginSample("CodeModule.ProcessCompilerMessages");

            var severity = Severity.None;
            if (compilationTask.Status == CompilationStatus.MissingDependency)
                severity = Severity.Warning;
            else if (compilerMessages.Any(m => m.type == CompilerMessageType.Error))
                severity = Severity.Error;

            var assemblyInfo = AssemblyInfoProvider.GetAssemblyInfoFromAssemblyPath(compilationTask.AssemblyPath);
            yield return context.CreateInsight(IssueCategory.Assembly, assemblyInfo.Name)
                .WithCustomProperties(new object[(int)AssemblyProperty.Num]
                {
                    assemblyInfo.IsPackageReadOnly,
                    compilationTask.DurationInMs
                })
                .WithDependencies(new AssemblyDependencyNode(assemblyInfo.Name,
                    compilationTask.Dependencies.Select(d => d.AssemblyName).ToArray()))
                .WithLocation(assemblyInfo.AsmDefPath)
                .WithSeverity(severity);

            foreach (var message in compilerMessages)
            {
                var relativePath = AssemblyInfoProvider.ResolveAssetPath(assemblyInfo, message.file);

                // stephenm TODO - A more data-driven way to specify which view Roslyn messages should be sent to, depending on their code.
                if (s_RegEx.IsMatch(message.code))
                {
                    var descriptor = new Descriptor(
                        message.code,
                        message.message,
                        Areas.IterationTime,
                        RoslynTextLookup.GetDescription(message.code),
                        RoslynTextLookup.GetRecommendation(message.code));

                    DescriptorLibrary.RegisterDescriptor(descriptor.Id, descriptor);

                    yield return context.CreateIssue(IssueCategory.DomainReload, descriptor.Id)
                        .WithLocation(relativePath, message.line)
                        .WithLogLevel(CompilerMessageTypeToLogLevel(message.type))
                        .WithCustomProperties(new object[(int)CompilerMessageProperty.Num]
                        {
                            message.code,
                            assemblyInfo.Name
                        });
                }
                else
                {
                    yield return context.CreateInsight(IssueCategory.CodeCompilerMessage, message.message)
                        .WithCustomProperties(new object[(int)CompilerMessageProperty.Num]
                        {
                            message.code,
                            assemblyInfo.Name
                        })
                        .WithLocation(relativePath, message.line)
                        .WithLogLevel(CompilerMessageTypeToLogLevel(message.type));
                }
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
