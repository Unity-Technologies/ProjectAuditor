using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.AssemblyUtils
{
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
        public string Code;
        /// <summary>
        ///   <para>Message type.</para>
        /// </summary>
        public CompilerMessageType Type;
        /// <summary>
        ///   <para>Message body.</para>
        /// </summary>
        public string Message;
        /// <summary>
        ///   <para>File for the message.</para>
        /// </summary>
        public string File;
        /// <summary>
        ///   <para>File line for the message.</para>
        /// </summary>
        public int Line;
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

        public string[] AssemblyNames;
        public CodeOptimization CodeOptimization = CodeOptimization.Release;
        public CompilationMode CompilationMode = CompilationMode.Player;
        public BuildTarget Platform = EditorUserBuildSettings.activeBuildTarget;
        public string[] RoslynAnalyzers;

        public Action<AssemblyCompilationTask, CompilerMessage[]> OnAssemblyCompilationFinished;

        public void Dispose()
        {
            if (!string.IsNullOrEmpty(m_OutputFolder) && Directory.Exists(m_OutputFolder))
            {
                foreach (var task in m_AssemblyCompilationTasks.Select(pair => pair.Value).Where(u => u.Success()))
                {
                    File.Delete(task.AssemblyPath);
                    File.Delete(Path.ChangeExtension(task.AssemblyPath, ".pdb"));
                }

                m_AssemblyCompilationTasks.Clear();

                // We can't delete the folder because of the CompilationLog.txt created by the AssemblyBuilder compilationTask
                //Directory.Delete(m_OutputFolder, true);
            }
            m_OutputFolder = string.Empty;
        }

        public AssemblyInfo[] Compile(IProgress progress = null)
        {
            var editorAssemblies = CompilationMode == CompilationMode.Editor || CompilationMode == CompilationMode.EditorPlayMode;
            var assemblies = GetAssemblies(editorAssemblies);

            if (AssemblyNames != null)
            {
                var assembliesAndDependencies = new List<Assembly>();
                foreach (var assembly in assemblies.Where(a => AssemblyNames.Contains(a.name)))
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
            var assemblies =
                CompilationPipeline.GetAssemblies(editorAssemblies
                    ? AssembliesType.Editor
                    : AssembliesType.PlayerWithoutTestAssemblies);

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
            if (CompilationMode == CompilationMode.EditorPlayMode)
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
                var assemblyName = assemblyInfo.Name;
                var compilationTask = m_AssemblyCompilationTasks[assemblyName];

                compilationTask.Messages = messages;

                if (progress != null)
                    progress.Advance(assemblyName);

                var stopWatch = compilationTask.StopWatch;
                if (stopWatch != null)
                    stopWatch.Stop();

                if (OnAssemblyCompilationFinished != null)
                    OnAssemblyCompilationFinished(compilationTask, messages);
            });
            UpdateAssemblyBuilders(progress);
            if (progress?.IsCancelled ?? false)
                return Array.Empty<string>();

            if (progress != null)
                progress.Clear();

            if (OnAssemblyCompilationFinished != null)
                foreach (var compilationTask in m_AssemblyCompilationTasks.Where(pair => pair.Value.Status == CompilationStatus.MissingDependency).Select(p => p.Value))
                {
                    OnAssemblyCompilationFinished(compilationTask, new CompilerMessage[] {});
                }

            return m_AssemblyCompilationTasks.Where(pair => pair.Value.Success()).Select(task => task.Value.AssemblyPath);
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
#pragma warning disable 618 // disable warning for obsolete AssemblyBuilder
                var assemblyBuilder = new AssemblyBuilder(assemblyPath, assembly.sourceFiles);
#pragma warning restore 618
                assemblyBuilder.buildTarget = Platform;
                assemblyBuilder.buildTargetGroup = BuildPipeline.GetBuildTargetGroup(Platform);
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
                                Message = messageBody,
                                File = originalMessages[i].file,
                                Line = originalMessages[i].line,
                                Code = messageParts[0]
                            };

                            // disregard originalMessages[i].type because it does not support CompilerMessageType.Info in 2020.x
                            switch (messageType)
                            {
                                case "error":
                                    messages[i].Type = CompilerMessageType.Error;
                                    break;
                                case "warning":
                                    messages[i].Type = CompilerMessageType.Warning;
                                    break;
                                case "info":
                                    messages[i].Type = CompilerMessageType.Info;
                                    break;
                            }
                        }
                        else
                        {
                            // Copy messages that don't have the standard format. We can't extract a code string from these.
                            messages[i] = new CompilerMessage
                            {
                                Message = originalMessages[i].message,
                                File = String.IsNullOrEmpty(originalMessages[i].file) ? PathUtils.GetDirectoryName(Application.dataPath) : originalMessages[i].file,
                                Line = originalMessages[i].line,
                                Code = "<Unity>"
                            };

                            switch (originalMessages[i].type)
                            {
                                case UnityEditor.Compilation.CompilerMessageType.Error:
                                    messages[i].Type = CompilerMessageType.Error;
                                    break;
                                case UnityEditor.Compilation.CompilerMessageType.Warning:
                                    messages[i].Type = CompilerMessageType.Warning;
                                    break;
#if UNITY_2021_1_OR_NEWER
                                case UnityEditor.Compilation.CompilerMessageType.Info:
                                    messages[i].Type = CompilerMessageType.Info;
                                    break;
#endif
                            }
                        }
                    }

                    assemblyCompilationFinished(path, messages);
                };
                assemblyBuilder.compilerOptions = new ScriptCompilerOptions
                {
                    AdditionalCompilerArguments = assembly.compilerOptions.AdditionalCompilerArguments,
                    AllowUnsafeCode = assembly.compilerOptions.AllowUnsafeCode,
                    ApiCompatibilityLevel = assembly.compilerOptions.ApiCompatibilityLevel,
                    CodeOptimization = CodeOptimization == CodeOptimization.Release ? UnityEditor.Compilation.CodeOptimization.Release : UnityEditor.Compilation.CodeOptimization.Debug, // assembly.compilerOptions.CodeOptimization,
                    RoslynAnalyzerDllPaths = RoslynAnalyzers ?? Array.Empty<string>()
                };

                switch (CompilationMode)
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

                assemblyBuilder.referencesOptions = ReferencesOptions.UseEngineModules;

                m_AssemblyCompilationTasks.Add(assemblyName, new AssemblyCompilationTask { Builder = assemblyBuilder });
            }

            // second pass: find all assembly reference builders
            foreach (var assembly in assemblies)
            {
                var dependencies = new List<AssemblyCompilationTask>();
                foreach (var referenceName in assembly.assemblyReferences.Select(r => Path.GetFileNameWithoutExtension(r.outputPath)))
                {
                    dependencies.Add(m_AssemblyCompilationTasks[referenceName]);
                }

                m_AssemblyCompilationTasks[Path.GetFileName(assembly.name)].Dependencies =
                    dependencies.ToArray();
            }
        }

        void UpdateAssemblyBuilders(IProgress progress)
        {
            while (true)
            {
                if (progress?.IsCancelled ?? false)
                    return; // compilation of assemblies will continue but we won't wait for it

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
