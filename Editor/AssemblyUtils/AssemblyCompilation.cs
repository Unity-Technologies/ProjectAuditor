using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.AssemblyUtils
{
    /// <summary>
    /// Options for the compilation mode Project Auditor should use when performing code analysis
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum CompilationMode
    {
        /// <summary>
        ///   <para>Non-Development player (default)</para>
        /// </summary>
        Player,
        /// <summary>
        ///   <para>Development player</para>
        /// </summary>
        DevelopmentPlayer,

        /// <summary>
        ///   <para>Editor assemblies for Play Mode</para>
        /// </summary>
        EditorPlayMode,

        /// <summary>
        ///   <para>Editor assemblies</para>
        /// </summary>
        Editor
    }

    /// <summary>
    /// Options for selecting the code optimization level to be used during code analysis
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum CodeOptimization
    {
        /// <summary>
        /// Debug code optimization
        /// </summary>
        Debug,

        /// <summary>
        /// Release code optimization
        /// </summary>
        Release
    }

    enum CompilerMessageType
    {
        /// <summary>
        ///   <para>Error message.</para>
        /// </summary>
        Error,
        /// <summary>
        ///   <para>Warning message.</para>
        /// </summary>
        Warning,
        /// <summary>
        ///   <para>Info message.</para>
        /// </summary>
        Info
    }

    struct CompilerMessage
    {
        /// <summary>
        ///   <para>Message code.</para>
        /// </summary>
        public string code;
        /// <summary>
        ///   <para>Message type.</para>
        /// </summary>
        public CompilerMessageType type;
        /// <summary>
        ///   <para>Message body.</para>
        /// </summary>
        public string message;
        /// <summary>
        ///   <para>File for the message.</para>
        /// </summary>
        public string file;
        /// <summary>
        ///   <para>File line for the message.</para>
        /// </summary>
        public int line;
    }

    enum CompilationStatus
    {
        NotStarted,
        IsCompiling,
        Compiled,
        MissingDependency
    }

    class AssemblyCompilation : IDisposable
    {
        Dictionary<string, AssemblyCompilationTask> m_AssemblyCompilationTasks;
        string m_OutputFolder = string.Empty;

        public string[] assemblyNames;
        public CodeOptimization codeOptimization = CodeOptimization.Release;
        public CompilationMode compilationMode = CompilationMode.Player;
        public BuildTarget platform = EditorUserBuildSettings.activeBuildTarget;
        public string[] roslynAnalyzers;

        public Action<AssemblyCompilationTask, CompilerMessage[]> onAssemblyCompilationFinished;

        public void Dispose()
        {
            if (!string.IsNullOrEmpty(m_OutputFolder) && Directory.Exists(m_OutputFolder))
            {
                foreach (var task in m_AssemblyCompilationTasks.Select(pair => pair.Value).Where(u => u.Success()))
                {
                    File.Delete(task.assemblyPath);
                    File.Delete(Path.ChangeExtension(task.assemblyPath, ".pdb"));
                }

                m_AssemblyCompilationTasks.Clear();

                // We can't delete the folder because of the CompilationLog.txt created by the AssemblyBuilder compilationTask
                //Directory.Delete(m_OutputFolder, true);
            }
            m_OutputFolder = string.Empty;
        }

        public AssemblyInfo[] Compile(IProgress progress = null)
        {
            var editorAssemblies = compilationMode == CompilationMode.Editor || compilationMode == CompilationMode.EditorPlayMode;
            var assemblies = GetAssemblies(editorAssemblies);

            if (assemblyNames != null)
            {
                var assembliesAndDependencies = new List<Assembly>();
                foreach (var assembly in assemblies.Where(a => assemblyNames.Contains(a.name)))
                {
                    CollectAssemblyDependencies(assembly, assembliesAndDependencies);
                }

                assemblies = assembliesAndDependencies.ToArray();
            }

            IEnumerable<string> compiledAssemblyPaths;
            if (editorAssemblies)
                compiledAssemblyPaths = GetEditorAssemblies(assemblies);
            else
                compiledAssemblyPaths = CompilePlayerAssemblies(assemblies, progress);

            return compiledAssemblyPaths.Select(AssemblyInfoProvider.GetAssemblyInfoFromAssemblyPath).ToArray();
        }

        static Assembly[] GetAssemblies(bool editorAssemblies)
        {
#if UNITY_2019_3_OR_NEWER
            var assemblies =
                CompilationPipeline.GetAssemblies(editorAssemblies
                    ? AssembliesType.Editor
                    : AssembliesType.PlayerWithoutTestAssemblies);
#else
            var assemblies =
                CompilationPipeline.GetAssemblies(editorAssemblies ? AssembliesType.Editor : AssembliesType.Player);
#endif
            return assemblies;
        }

        static void CollectAssemblyDependencies(Assembly assembly, List<Assembly> assembliesAndDependencies)
        {
            if (!assembliesAndDependencies.Contains(assembly))
                assembliesAndDependencies.Add(assembly);
            var missingDependencies = assembly.assemblyReferences.Where(d => !assembliesAndDependencies.Contains(d));
            foreach (var dependency in missingDependencies)
            {
                CollectAssemblyDependencies(dependency, assembliesAndDependencies);
            }
        }

        IEnumerable<string> GetEditorAssemblies(IEnumerable<Assembly> assemblies)
        {
            if (compilationMode == CompilationMode.EditorPlayMode)
            {
                // exclude Editor-Only Assemblies
                assemblies = assemblies.Where(a => a.flags != AssemblyFlags.EditorAssembly);
            }
            return assemblies.Select(assembly => assembly.outputPath);
        }

        public static IEnumerable<string> GetAssemblyReferencePaths(CompilationMode compilationMode)
        {
            var editorAssemblies = compilationMode == CompilationMode.Editor || compilationMode == CompilationMode.EditorPlayMode;
            var paths = GetAssemblies(editorAssemblies)
                .SelectMany(a => a.compiledAssemblyReferences).Select(Path.GetDirectoryName).Distinct();
            return paths;
        }

        IEnumerable<string> CompilePlayerAssemblies(Assembly[] assemblies, IProgress progress = null)
        {
            if (progress != null)
            {
                var numAssemblies = assemblies.Length;
                progress.Start("Assembly Compilation", "Compiling project scripts",
                    numAssemblies);
            }

            m_OutputFolder = FileUtil.GetUniqueTempPathInProject();

            if (!Directory.Exists(m_OutputFolder))
                Directory.CreateDirectory(m_OutputFolder);

            PrepareAssemblyBuilders(assemblies, (assemblyPath, messages) =>
            {
                var assemblyInfo = AssemblyInfoProvider.GetAssemblyInfoFromAssemblyPath(assemblyPath);
                var assemblyName = assemblyInfo.name;
                var compilationTask = m_AssemblyCompilationTasks[assemblyName];

                compilationTask.messages = messages;

                if (progress != null)
                    progress.Advance(assemblyName);

                var stopWatch = compilationTask.stopWatch;
                if (stopWatch != null)
                    stopWatch.Stop();

                if (onAssemblyCompilationFinished != null)
                    onAssemblyCompilationFinished(compilationTask, messages);
            });
            UpdateAssemblyBuilders();

            if (progress != null)
                progress.Clear();

            if (onAssemblyCompilationFinished != null)
                foreach (var compilationTask in m_AssemblyCompilationTasks.Where(pair => pair.Value.status == CompilationStatus.MissingDependency).Select(p => p.Value))
                {
                    onAssemblyCompilationFinished(compilationTask, new CompilerMessage[] {});
                }

            return m_AssemblyCompilationTasks.Where(pair => pair.Value.Success()).Select(task => task.Value.assemblyPath);
        }

        void PrepareAssemblyBuilders(Assembly[] assemblies, Action<string, CompilerMessage[]> assemblyCompilationFinished)
        {
            m_AssemblyCompilationTasks = new Dictionary<string, AssemblyCompilationTask>();
            // first pass: create all compilation tasks
            foreach (var assembly in assemblies)
            {
                var filename = Path.GetFileName(assembly.outputPath);
                var assemblyName = Path.GetFileNameWithoutExtension(assembly.outputPath);
                var assemblyPath = Path.Combine(m_OutputFolder, filename);
                var assemblyBuilder = new AssemblyBuilder(assemblyPath, assembly.sourceFiles);

                assemblyBuilder.buildTarget = platform;
                assemblyBuilder.buildTargetGroup = BuildPipeline.GetBuildTargetGroup(platform);
                assemblyBuilder.buildFinished += (path, originalMessages) =>
                {
                    var messages = new CompilerMessage[originalMessages.Length];
                    for (int i = 0; i < originalMessages.Length; i++)
                    {
                        var messageStartIndex = originalMessages[i].message.LastIndexOf("):");
                        if (messageStartIndex != -1)
                        {
                            var messageWithCode = originalMessages[i].message.Substring(messageStartIndex + 2);
                            var messageParts = messageWithCode.Split(new[] {' ', ':'}, 2,
                                StringSplitOptions.RemoveEmptyEntries);
                            if (messageParts.Length < 2)
                                continue;

                            var messageType = messageParts[0];
                            if (messageParts[1].IndexOf(':') == -1)
                                continue;

                            messageParts = messageParts[1].Split(':');
                            if (messageParts.Length < 2)
                                continue;

                            var messageBody = messageWithCode.Substring(messageWithCode.IndexOf(": ", StringComparison.Ordinal) + 2);
                            messages[i] = new CompilerMessage
                            {
                                message = messageBody,
                                file = originalMessages[i].file,
                                line = originalMessages[i].line,
                                code = messageParts[0]
                            };

                            // disregard originalMessages[i].type because it does not support CompilerMessageType.Info in 2020.x
                            switch (messageType)
                            {
                                case "error":
                                    messages[i].type = CompilerMessageType.Error;
                                    break;
                                case "warning":
                                    messages[i].type = CompilerMessageType.Warning;
                                    break;
                                case "info":
                                    messages[i].type = CompilerMessageType.Info;
                                    break;
                            }
                        }
                        else
                        {
                            // Copy messages that don't have the standard format. We can't extract a code string from these.
                            messages[i] = new CompilerMessage
                            {
                                message = originalMessages[i].message,
                                file = String.IsNullOrEmpty(originalMessages[i].file) ? PathUtils.GetDirectoryName(Application.dataPath) : originalMessages[i].file,
                                line = originalMessages[i].line,
                                code = "<Unity>"
                            };

                            switch (originalMessages[i].type)
                            {
                                case UnityEditor.Compilation.CompilerMessageType.Error:
                                    messages[i].type = CompilerMessageType.Error;
                                    break;
                                case UnityEditor.Compilation.CompilerMessageType.Warning:
                                    messages[i].type = CompilerMessageType.Warning;
                                    break;
#if UNITY_2021_1_OR_NEWER
                                case UnityEditor.Compilation.CompilerMessageType.Info:
                                    messages[i].type = CompilerMessageType.Info;
                                    break;
#endif
                            }
                        }
                    }

                    assemblyCompilationFinished(path, messages);
                };
#if UNITY_2020_2_OR_NEWER
                assemblyBuilder.compilerOptions = new ScriptCompilerOptions
                {
                    AdditionalCompilerArguments = assembly.compilerOptions.AdditionalCompilerArguments,
                    AllowUnsafeCode = assembly.compilerOptions.AllowUnsafeCode,
                    ApiCompatibilityLevel = assembly.compilerOptions.ApiCompatibilityLevel,
                    CodeOptimization = codeOptimization == CodeOptimization.Release ? UnityEditor.Compilation.CodeOptimization.Release : UnityEditor.Compilation.CodeOptimization.Debug, // assembly.compilerOptions.CodeOptimization,
                    RoslynAnalyzerDllPaths = roslynAnalyzers ?? Array.Empty<string>()
                };
#else
                assemblyBuilder.compilerOptions = assembly.compilerOptions;
#endif

                switch (compilationMode)
                {
                    case CompilationMode.Player:
                        assemblyBuilder.flags = AssemblyBuilderFlags.None;
                        break;
                    case CompilationMode.DevelopmentPlayer:
                        assemblyBuilder.flags = AssemblyBuilderFlags.DevelopmentBuild;
                        break;
                    case CompilationMode.Editor:
                        assemblyBuilder.flags = AssemblyBuilderFlags.EditorAssembly;
                        break;
                }

                // add asmdef-specific defines
                var additionalDefines = new List<string>(assembly.defines.Except(assemblyBuilder.defaultDefines));

                // temp fix for UWP compilation error (failing to find references to Windows SDK assemblies)
                additionalDefines.Remove("ENABLE_WINMD_SUPPORT");
                additionalDefines.Remove("WINDOWS_UWP");

                additionalDefines.Add("ENABLE_UNITY_COLLECTIONS_CHECKS");
                assemblyBuilder.additionalDefines = additionalDefines.ToArray();

                // add references to assemblies we need to build
                assemblyBuilder.additionalReferences = assembly.assemblyReferences.Select(r => Path.Combine(m_OutputFolder, Path.GetFileName(r.outputPath))).ToArray();

                // exclude all assemblies that we are building ourselves to a Temp folder
                assemblyBuilder.excludeReferences =
                    assemblyBuilder.defaultReferences.Where(r => r.StartsWith("Library")).ToArray();

#if UNITY_2019_1_OR_NEWER
                assemblyBuilder.referencesOptions = ReferencesOptions.UseEngineModules;
#endif
                m_AssemblyCompilationTasks.Add(assemblyName, new AssemblyCompilationTask { builder = assemblyBuilder });
            }

            // second pass: find all assembly reference builders
            foreach (var assembly in assemblies)
            {
                var dependencies = new List<AssemblyCompilationTask>();
                foreach (var referenceName in assembly.assemblyReferences.Select(r => Path.GetFileNameWithoutExtension(r.outputPath)))
                {
                    dependencies.Add(m_AssemblyCompilationTasks[referenceName]);
                }

                m_AssemblyCompilationTasks[Path.GetFileName(assembly.name)].dependencies =
                    dependencies.ToArray();
            }
        }

        void UpdateAssemblyBuilders()
        {
            while (true)
            {
                var pendingTasks = m_AssemblyCompilationTasks.Select(pair => pair.Value).Where(task => !task.IsDone());
                if (!pendingTasks.Any())
                    break;
                foreach (var task in pendingTasks)
                {
                    task.Update();
                }
                System.Threading.Thread.Sleep(10);
            }
        }
    }
}
