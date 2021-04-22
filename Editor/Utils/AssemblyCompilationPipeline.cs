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
        public AssemblyCompilationUnit[] dependencies;
        public CompilerMessage[] messages;

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
                            builder.Build(); // all references are built, we can kick off this builder
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

        public Action<string, CompilerMessage[]> AssemblyCompilationFinished;

        public void Dispose()
        {
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
            compiledAssemblyPaths = CompileAssemblies(assemblies, editorAssemblies, progressBar);
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
        IEnumerable<string> CompileAssemblies(Assembly[] assemblies, bool editorAssemblies, IProgressBar progressBar = null)
        {
            if (progressBar != null)
            {
                var numAssemblies = assemblies.Length;
                progressBar.Initialize("Assembly Compilation", "Compiling project scripts",
                    numAssemblies);
            }

            m_OutputFolder = FileUtil.GetUniqueTempPathInProject();

            if (!Directory.Exists(m_OutputFolder))
                Directory.CreateDirectory(m_OutputFolder);

            PrepareAssemblyBuilders(assemblies, editorAssemblies, (assemblyPath, messages) =>
            {
                var assemblyName = Path.GetFileNameWithoutExtension(assemblyPath);
                m_AssemblyCompilationUnits[assemblyName].messages = messages;

                if (progressBar != null)
                    progressBar.AdvanceProgressBar(assemblyName);

                if (AssemblyCompilationFinished != null)
                    AssemblyCompilationFinished(assemblyName, messages);
            });
            UpdateAssemblyBuilders();

            if (progressBar != null)
                progressBar.ClearProgressBar();

            return m_AssemblyCompilationUnits.Where(pair => pair.Value.Success()).Select(unit => unit.Value.assemblyPath);
        }

        void PrepareAssemblyBuilders(Assembly[] assemblies, bool editorAssemblies, Action<string, CompilerMessage[]> assemblyCompilationFinished)
        {
            m_AssemblyCompilationUnits = new Dictionary<string, AssemblyCompilationUnit>();

            // first pass: create all AssemblyCompilationUnits
            foreach (var assembly in assemblies)
            {
                var filename = Path.GetFileName(assembly.outputPath);
                var assemblyName = Path.GetFileNameWithoutExtension(assembly.outputPath);
                var assemblyPath = Path.Combine(m_OutputFolder, filename);
                var assemblyBuilder = new AssemblyBuilder(assemblyPath, assembly.sourceFiles);

                assemblyBuilder.buildFinished += assemblyCompilationFinished;
                assemblyBuilder.compilerOptions = assembly.compilerOptions;
                assemblyBuilder.flags = editorAssemblies ? AssemblyBuilderFlags.EditorAssembly : AssemblyBuilderFlags.DevelopmentBuild;

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
