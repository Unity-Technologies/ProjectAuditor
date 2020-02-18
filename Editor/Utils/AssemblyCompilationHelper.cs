using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Player;
using UnityEditor.Compilation;

namespace Unity.ProjectAuditor.Editor.Utils
{
    public class AssemblyCompilationHelper : IDisposable
    {
        private bool m_Success = true;
        private string m_OutputFolder = null;

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
                @group = EditorUserBuildSettings.selectedBuildTargetGroup
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
        
        public void Dispose()
        {
#if UNITY_2018_2_OR_NEWER
            CompilationPipeline.assemblyCompilationFinished -= OnAssemblyCompilationFinished;
#endif
            if (!string.IsNullOrEmpty(m_OutputFolder))
            {
                Directory.Delete(m_OutputFolder, true);
            }
        }
        
        public IEnumerable<string> GetCompiledAssemblyDirectories()
        {
#if UNITY_2018_2_OR_NEWER
            yield return m_OutputFolder;
#else
            yield return CompilationPipeline.GetAssemblies()
                .Where(a => a.flags != AssemblyFlags.EditorAssembly).Select(assembly => assembly.outputPath)
#endif
        }

        private void OnAssemblyCompilationFinished(string outputAssemblyPath, CompilerMessage[] messages)
        {
            m_Success = m_Success && messages.Count(message => message.type == CompilerMessageType.Error) == 0;
        }
    }
}