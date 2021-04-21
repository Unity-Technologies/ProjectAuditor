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
    class AssemblyCompilationPipeline : IDisposable
    {
        string m_OutputFolder = string.Empty;
        bool m_Success = true;

        Action<string> m_OnAssemblyCompilationStarted;

        public void Dispose()
        {
#if UNITY_2018_2_OR_NEWER
            if (m_OnAssemblyCompilationStarted != null)
                CompilationPipeline.assemblyCompilationStarted -= m_OnAssemblyCompilationStarted;

            CompilationPipeline.assemblyCompilationFinished -= OnAssemblyCompilationFinished;
#endif
            if (!string.IsNullOrEmpty(m_OutputFolder) && Directory.Exists(m_OutputFolder))
            {
                Directory.Delete(m_OutputFolder, true);
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

            var assemblyBuilders = PrepareAssemblyBuilders(assemblies);

            while (assemblyBuilders.Any(pair => pair.Value.status != AssemblyBuilderStatus.Finished))
            {
                UpdateAssemblyBuilders(assemblyBuilders);
                System.Threading.Thread.Sleep(10);
            }

            if (progressBar != null)
                progressBar.ClearProgressBar();

            if (!m_Success)
            {
                Dispose();
                throw new AssemblyCompilationException();
            }

            return assemblyBuilders.Select(pair => pair.Value.assemblyPath);
        }

        Dictionary<string, AssemblyBuilder> PrepareAssemblyBuilders(Assembly[] assemblies)
        {
            var assemblyBuilders = new Dictionary<string, AssemblyBuilder>();
            foreach (var assembly in assemblies)
            {
                var filename = Path.GetFileName(assembly.outputPath);
                var assemblyName = Path.GetFileNameWithoutExtension(assembly.outputPath);
                var assemblyPath = Path.Combine(m_OutputFolder, filename);
                var assemblyBuilder = new AssemblyBuilder(assemblyPath, assembly.sourceFiles);

                assemblyBuilder.buildFinished += OnAssemblyCompilationFinished;
                assemblyBuilder.compilerOptions = assembly.compilerOptions;

                var references = assembly.assemblyReferences.Select(r => Path.Combine(m_OutputFolder, Path.GetFileName(r.outputPath))).ToList();
                references.AddRange(assembly.compiledAssemblyReferences);
                assemblyBuilder.additionalReferences = references.ToArray();

                assemblyBuilders.Add(assemblyName, assemblyBuilder);
            }

            return assemblyBuilders;
        }

        void UpdateAssemblyBuilders(Dictionary<string, AssemblyBuilder> assemblyBuilders)
        {
            foreach (var pair in assemblyBuilders.Where(b => b.Value.status == AssemblyBuilderStatus.NotStarted))
            {
                var builder = pair.Value;
                if (builder.additionalReferences.Select(path => Path.GetFileNameWithoutExtension(path)).All(name => !assemblyBuilders.ContainsKey(name) || assemblyBuilders[name].status == AssemblyBuilderStatus.Finished))
                {
                    //Debug.Log("Compiling " + builder.assemblyPath);
                    builder.Build();
                }
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

        void OnAssemblyCompilationFinished(string outputAssemblyPath, CompilerMessage[] messages)
        {
            foreach (var m in messages)
            {
                if (m.type == CompilerMessageType.Error)
                    Debug.LogError(m.message);
            }
            m_Success = m_Success && messages.Count(message => message.type == CompilerMessageType.Error) == 0;
        }
    }
}
