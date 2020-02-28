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
    public class AssemblyCompilationHelper : IDisposable
    {
        private string m_OutputFolder = String.Empty;
        private bool m_Success = true;

        public void Dispose()
        {
#if UNITY_2018_2_OR_NEWER
            CompilationPipeline.assemblyCompilationFinished -= OnAssemblyCompilationFinished;
#endif
            if (!string.IsNullOrEmpty(m_OutputFolder)) Directory.Delete(m_OutputFolder, true);
        }

        public IEnumerable<string> Compile()
        {
            if (EditorUtility.scriptCompilationFailed)
                throw new AssemblyCompilationException();
#if UNITY_2018_2_OR_NEWER
            m_OutputFolder = FileUtil.GetUniqueTempPathInProject();

            CompilationPipeline.assemblyCompilationFinished += OnAssemblyCompilationFinished;

            var input = new ScriptCompilationSettings
            {
                target = EditorUserBuildSettings.activeBuildTarget,
                group = EditorUserBuildSettings.selectedBuildTargetGroup
            };

            var compilationResult = PlayerBuildInterface.CompilePlayerScripts(input, m_OutputFolder);

            if (!m_Success)
                throw new AssemblyCompilationException();

            return compilationResult.assemblies.Select(assembly => Path.Combine(m_OutputFolder, assembly));
#else
            // fallback to CompilationPipeline assemblies 
            return CompilationPipeline.GetAssemblies()
                .Where(a => a.flags != AssemblyFlags.EditorAssembly).Select(assembly => assembly.outputPath);
#endif
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

        private void OnAssemblyCompilationFinished(string outputAssemblyPath, CompilerMessage[] messages)
        {
            m_Success = m_Success && messages.Count(message => message.type == CompilerMessageType.Error) == 0;
        }
    }
}