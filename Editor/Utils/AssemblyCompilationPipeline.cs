using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Packages.Editor.Utils;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
#if UNITY_2018_2_OR_NEWER
using UnityEditor.Build.Player;

#endif

namespace Unity.ProjectAuditor.Editor.Utils
{
    class AssemblyCompilationUnit
    {
        public AssemblyBuilder builder;
        public AssemblyBuilder[] referenceBuilders;
        public CompilerMessage[] messages;

        public string assemblyPath
        {
            get
            {
                return builder.assemblyPath;
            }
        }

        public bool IsDone()
        {
            return builder.status == AssemblyBuilderStatus.Finished;
        }

        public void Update()
        {
            if (builder.status != AssemblyBuilderStatus.NotStarted)
                return; // nothing to do

            // if all references are finished, we can kick off this builder
            if (referenceBuilders.All(builder => builder.status == AssemblyBuilderStatus.Finished))
                builder.Build();
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
        Action<string> m_OnAssemblyCompilationStarted;

        public Action<string, CompilerMessage[]> AssemblyCompilationFinished;

        public void Dispose()
        {
#if UNITY_2018_2_OR_NEWER
            if (m_OnAssemblyCompilationStarted != null)
                CompilationPipeline.assemblyCompilationStarted -= m_OnAssemblyCompilationStarted;

            CompilationPipeline.assemblyCompilationFinished -= OnAssemblyCompilationFinished;
#endif
            if (!string.IsNullOrEmpty(m_OutputFolder) && Directory.Exists(m_OutputFolder))
            {
//TEMP                Directory.Delete(m_OutputFolder, true);
            }
            m_OutputFolder = string.Empty;
        }

        public IEnumerable<AssemblyInfo> Compile(bool editorAssemblies = false, IProgressBar progressBar = null)
        {
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
            {
                compiledAssemblyPaths = CompileEditorAssemblies(assemblies, false);
            }
            else
            {
                compiledAssemblyPaths = CompilePlayerAssemblies(assemblies, progressBar);
            }
#else
            // fallback to CompilationPipeline assemblies
            compiledAssemblyPaths = CompileEditorAssemblies(assemblies, !editorAssemblies);
#endif

            var assemblyInfos = new List<AssemblyInfo>();
            foreach (var compiledAssemblyPath in compiledAssemblyPaths)
            {
                var assemblyInfo = AssemblyInfoProvider.GetAssemblyInfoFromAssemblyPath(compiledAssemblyPath);
                var assembly = assemblies.First(a => a.name.Equals(assemblyInfo.name));
                var sourcePaths = assembly.sourceFiles.Select(file => file.Remove(0, assemblyInfo.relativePath.Length + 1));

                assemblyInfo.sourcePaths = sourcePaths.ToArray();
                assemblyInfos.Add(assemblyInfo);
            }

            return assemblyInfos;
        }

        IEnumerable<string> CompileEditorAssemblies(IEnumerable<Assembly> assemblies, bool excludeEditorOnlyAssemblies)
        {
            if (excludeEditorOnlyAssemblies)
            {
                assemblies = assemblies.Where(a => a.flags != AssemblyFlags.EditorAssembly);
            }
            return assemblies.Select(assembly => assembly.outputPath);
        }

#if UNITY_2018_2_OR_NEWER
        IEnumerable<string> CompilePlayerAssemblies(Assembly[] assemblies, IProgressBar progressBar = null)
        {
            if (progressBar != null)
            {
                var numAssemblies = assemblies.Length;
                progressBar.Initialize("Assembly Compilation", "Compiling project scripts",
                    numAssemblies);
                m_OnAssemblyCompilationStarted = s =>
                {
                    // The compilation pipeline might compile Editor-specific assemblies
                    // let's advance the progress bar only for Player ones.
                    var assemblyName = Path.GetFileNameWithoutExtension(s);
                    if (assemblies.FirstOrDefault(asm => asm.name.Equals(assemblyName)) != null)
                        progressBar.AdvanceProgressBar(assemblyName);
                };
                CompilationPipeline.assemblyCompilationStarted += m_OnAssemblyCompilationStarted;
            }
            CompilationPipeline.assemblyCompilationFinished += OnAssemblyCompilationFinished;

            m_OutputFolder = FileUtil.GetUniqueTempPathInProject();

            if (!Directory.Exists(m_OutputFolder))
                Directory.CreateDirectory(m_OutputFolder);

            PrepareAssemblyBuilders(assemblies);
            UpdateAssemblyBuilders();

            if (progressBar != null)
                progressBar.ClearProgressBar();

            return m_AssemblyCompilationUnits.Where(pair => pair.Value.Success()).Select(unit => unit.Value.assemblyPath);
        }

        void PrepareAssemblyBuilders(Assembly[] assemblies)
        {
            m_AssemblyCompilationUnits = new Dictionary<string, AssemblyCompilationUnit>();

            // first pass: create all AssemblyCompilationUnits
            foreach (var assembly in assemblies)
            {
                var filename = Path.GetFileName(assembly.outputPath);
                var assemblyName = Path.GetFileNameWithoutExtension(assembly.outputPath);
                var assemblyPath = Path.Combine(m_OutputFolder, filename);
                var assemblyBuilder = new AssemblyBuilder(assemblyPath, assembly.sourceFiles);

                assemblyBuilder.buildFinished += OnAssemblyCompilationFinished;
                assemblyBuilder.compilerOptions = assembly.compilerOptions;
                assemblyBuilder.flags = AssemblyBuilderFlags.DevelopmentBuild;

                // add asmdef-specific defines
                assemblyBuilder.additionalDefines = assembly.defines.Except(assemblyBuilder.defaultDefines).ToArray();

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
                var referenceBuilders = new List<AssemblyBuilder>();
                foreach (var referenceName in assembly.assemblyReferences.Select(r => Path.GetFileNameWithoutExtension(r.outputPath)))
                {
                    referenceBuilders.Add(m_AssemblyCompilationUnits[referenceName].builder);
                }

                m_AssemblyCompilationUnits[Path.GetFileName(assembly.name)].referenceBuilders =
                    referenceBuilders.ToArray();
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

        void OnAssemblyCompilationFinished(string assemblyPath, CompilerMessage[] messages)
        {
            var assemblyName = Path.GetFileNameWithoutExtension(assemblyPath);
            m_AssemblyCompilationUnits[assemblyName].messages = messages;

            if (AssemblyCompilationFinished != null)
                AssemblyCompilationFinished(assemblyName, messages);
        }
    }
}
