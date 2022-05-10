using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
#if UNITY_2018_2_OR_NEWER
using UnityEditor.Build.Player;

#endif

namespace Unity.ProjectAuditor.Editor.Utils
{
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
        ///   <para>Editor</para>
        /// </summary>
        Editor
    }

    enum CodeOptimization
    {
        Debug,
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

    class AssemblyCompilationUnit
    {
        public AssemblyBuilder builder;
        public AssemblyCompilationUnit[] dependencies;
        public CompilerMessage[] messages;
        public Stopwatch stopWatch;

        bool m_Done = false;

        public string assemblyPath
        {
            get
            {
                return builder.assemblyPath;
            }
        }

        public bool IsDone()
        {
            return m_Done;
        }

        public void Update()
        {
            switch (builder.status)
            {
                case AssemblyBuilderStatus.NotStarted:
                    if (dependencies.All(dep => dep.IsDone()))
                    {
                        if (dependencies.All(dep => dep.Success()))
                        {
                            stopWatch = Stopwatch.StartNew();
                            builder.Build(); // all references are built, we can kick off this builder
                        }
                        else
                            m_Done = true; // this assembly won't be built since it's missing dependencies
                    }
                    break;
                case AssemblyBuilderStatus.IsCompiling:
                    return; // nothing to do
                case AssemblyBuilderStatus.Finished:
                    m_Done = true;
                    break;
            }
        }

        public bool Success()
        {
            if (messages == null)
                return false;
            return messages.All(message => message.type != CompilerMessageType.Error);
        }
    }

    class AssemblyCompilationPipeline : IDisposable
    {
        string m_OutputFolder = string.Empty;

        Dictionary<string, AssemblyCompilationUnit> m_AssemblyCompilationUnits;
#if UNITY_2020_2_OR_NEWER
        string[] m_RoslynAnalyzers;
#endif

        public Action<AssemblyInfo, CompilerMessage[], long> AssemblyCompilationFinished;
        public CompilationMode CompilationMode;

        public static CodeOptimization CodeOptimization = CodeOptimization.Release;

        public AssemblyCompilationPipeline()
        {
#if UNITY_2020_2_OR_NEWER
            m_RoslynAnalyzers = AssetDatabase.FindAssets("l:RoslynAnalyzer").Select(AssetDatabase.GUIDToAssetPath).ToArray();
#endif
        }

        public void Dispose()
        {
            if (!string.IsNullOrEmpty(m_OutputFolder) && Directory.Exists(m_OutputFolder))
            {
                foreach (var unit in m_AssemblyCompilationUnits.Select(pair => pair.Value).Where(u => u.Success()))
                {
                    File.Delete(unit.assemblyPath);
                    File.Delete(Path.ChangeExtension(unit.assemblyPath, ".pdb"));
                }

                m_AssemblyCompilationUnits.Clear();

                // We can't delete the folder because of the CompilationLog.txt created by the AssemblyBuilder compilationTask
                //Directory.Delete(m_OutputFolder, true);
            }
            m_OutputFolder = string.Empty;
        }

        public AssemblyInfo[] Compile(IProgress progress = null)
        {
            var editorAssemblies = CompilationMode == CompilationMode.Editor;
#if UNITY_2019_3_OR_NEWER
            var assemblies = CompilationPipeline.GetAssemblies(editorAssemblies ? AssembliesType.Editor : AssembliesType.PlayerWithoutTestAssemblies);
#elif UNITY_2018_1_OR_NEWER
            var assemblies = CompilationPipeline.GetAssemblies(editorAssemblies ? AssembliesType.Editor : AssembliesType.Player);
#else
            var assemblies = CompilationPipeline.GetAssemblies();
#endif

            IEnumerable<string> compiledAssemblyPaths;
#if UNITY_2018_2_OR_NEWER
            if (editorAssemblies)
                compiledAssemblyPaths = CompileEditorAssemblies(assemblies);
            else
                compiledAssemblyPaths = CompilePlayerAssemblies(assemblies, progress);
#else
            // fallback to CompilationPipeline assemblies
            compiledAssemblyPaths = CompileEditorAssemblies(assemblies, !editorAssemblies);
#endif

            return compiledAssemblyPaths.Select(AssemblyInfoProvider.GetAssemblyInfoFromAssemblyPath).ToArray();
        }

        IEnumerable<string> CompileEditorAssemblies(IEnumerable<Assembly> assemblies)
        {
            // exclude Editor-Only Assemblies
            assemblies = assemblies.Where(a => a.flags != AssemblyFlags.EditorAssembly);
            return assemblies.Select(assembly => assembly.outputPath);
        }

#if UNITY_2018_2_OR_NEWER
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
                m_AssemblyCompilationUnits[assemblyName].messages = messages;

                if (progress != null)
                    progress.Advance(assemblyName);

                var stopWatch = m_AssemblyCompilationUnits[assemblyName].stopWatch;
                var elapsedTime = stopWatch != null
                    ? stopWatch.ElapsedMilliseconds
                    : 0;
                if (AssemblyCompilationFinished != null)
                    AssemblyCompilationFinished(assemblyInfo, messages, elapsedTime);
            });
            UpdateAssemblyBuilders();

            if (progress != null)
                progress.Clear();

            return m_AssemblyCompilationUnits.Where(pair => pair.Value.Success()).Select(unit => unit.Value.assemblyPath);
        }

        void PrepareAssemblyBuilders(Assembly[] assemblies, Action<string, CompilerMessage[]> assemblyCompilationFinished)
        {
            m_AssemblyCompilationUnits = new Dictionary<string, AssemblyCompilationUnit>();
            // first pass: create all AssemblyCompilationUnits
            foreach (var assembly in assemblies)
            {
                var filename = Path.GetFileName(assembly.outputPath);
                var assemblyName = Path.GetFileNameWithoutExtension(assembly.outputPath);
                var assemblyPath = Path.Combine(m_OutputFolder, filename);
                var assemblyBuilder = new AssemblyBuilder(assemblyPath, assembly.sourceFiles);

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

                            var messageBody = messageWithCode.Substring(messageWithCode.IndexOf(": ") + 2);
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
                    }

                    assemblyCompilationFinished(path, messages);
                };
#if UNITY_2020_2_OR_NEWER
                assemblyBuilder.compilerOptions = new ScriptCompilerOptions
                {
                    AdditionalCompilerArguments = assembly.compilerOptions.AdditionalCompilerArguments,
                    AllowUnsafeCode = assembly.compilerOptions.AllowUnsafeCode,
                    ApiCompatibilityLevel = assembly.compilerOptions.ApiCompatibilityLevel,
                    CodeOptimization = CodeOptimization == CodeOptimization.Release ? UnityEditor.Compilation.CodeOptimization.Release : UnityEditor.Compilation.CodeOptimization.Debug, // assembly.compilerOptions.CodeOptimization,
                    RoslynAnalyzerDllPaths = m_RoslynAnalyzers
                };
#else
                assemblyBuilder.compilerOptions = assembly.compilerOptions;
#endif

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

#if UNITY_2019_1_OR_NEWER
                assemblyBuilder.referencesOptions = ReferencesOptions.UseEngineModules;
#endif
                m_AssemblyCompilationUnits.Add(assemblyName, new AssemblyCompilationUnit { builder = assemblyBuilder });
            }

            // second pass: find all assembly reference builders
            foreach (var assembly in assemblies)
            {
                var dependencies = new List<AssemblyCompilationUnit>();
                foreach (var referenceName in assembly.assemblyReferences.Select(r => Path.GetFileNameWithoutExtension(r.outputPath)))
                {
                    dependencies.Add(m_AssemblyCompilationUnits[referenceName]);
                }

                m_AssemblyCompilationUnits[Path.GetFileName(assembly.name)].dependencies =
                    dependencies.ToArray();
            }
        }

        void UpdateAssemblyBuilders()
        {
            while (true)
            {
                var pendingUnits = m_AssemblyCompilationUnits.Select(pair => pair.Value).Where(unit => !unit.IsDone());
                if (!pendingUnits.Any())
                    break;
                foreach (var unit in pendingUnits)
                {
                    unit.Update();
                }
                System.Threading.Thread.Sleep(10);
            }
        }

#endif
        public IEnumerable<string> GetCompiledAssemblyDirectories()
        {
#if UNITY_2018_2_OR_NEWER
            yield return m_OutputFolder;
#else
            foreach (var dir in CompilationPipeline.GetAssemblies()
                     .Where(a => a.flags != AssemblyFlags.EditorAssembly).Select(assembly => Path.GetDirectoryName(assembly.outputPath)).Distinct())
            {
                yield return dir;
            }
#endif
        }
    }
}
