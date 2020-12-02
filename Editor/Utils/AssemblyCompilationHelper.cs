using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
#if UNITY_2018_2_OR_NEWER
using UnityEditor.Build.Player;

#endif

namespace Unity.ProjectAuditor.Editor.Utils
{
    class AssemblyCompilationHelper : IDisposable
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

        public IEnumerable<AssemblyInfo> Compile(IProgressBar progressBar = null)
        {
#if UNITY_2018_1_OR_NEWER
            var assemblies = CompilationPipeline.GetAssemblies(AssembliesType.PlayerWithoutTestAssemblies);
#else
            var assemblies = CompilationPipeline.GetAssemblies();
#endif

#if UNITY_2018_2_OR_NEWER
            if (progressBar != null)
            {
                var numAssemblies = assemblies.Length;
                progressBar.Initialize("Assembly Compilation", "Compiling project scripts",
                    numAssemblies);
                m_OnAssemblyCompilationStarted = (s) =>
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

            var input = new ScriptCompilationSettings
            {
                target = EditorUserBuildSettings.activeBuildTarget,
                group = EditorUserBuildSettings.selectedBuildTargetGroup
            };

            var compilationResult = PlayerBuildInterface.CompilePlayerScripts(input, m_OutputFolder);

            if (progressBar != null)
                progressBar.ClearProgressBar();

            if (!m_Success)
            {
                Dispose();
                throw new AssemblyCompilationException();
            }

            var compiledAssemblyPaths = compilationResult.assemblies.Select(assembly => Path.Combine(m_OutputFolder, assembly));
#else
            // fallback to CompilationPipeline assemblies
            var compiledAssemblyPaths = CompilationPipeline.GetAssemblies()
                .Where(a => a.flags != AssemblyFlags.EditorAssembly).Select(assembly => assembly.outputPath);
#endif

            var assemblyInfos = new List<AssemblyInfo>();
            foreach (var compiledAssemblyPath in compiledAssemblyPaths)
            {
                var assemblyInfo = AssemblyHelper.GetAssemblyInfoFromAssemblyPath(compiledAssemblyPath);
                var assembly = assemblies.First(a => a.name.Equals(assemblyInfo.name));
                var sourcePaths = assembly.sourceFiles.Select(file => file.Remove(0, assemblyInfo.relativePath.Length + 1));

                assemblyInfo.sourcePaths = sourcePaths.ToArray();
                assemblyInfos.Add(assemblyInfo);
            }

            return assemblyInfos;
        }

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
            m_Success = m_Success && messages.Count(message => message.type == CompilerMessageType.Error) == 0;
        }
    }
}
