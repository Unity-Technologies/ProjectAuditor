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
    public class AssemblyHelper
    {
        static private  List<string> compiledAssemblyPaths = new List<string>();

        static public bool CompileAssemblies()
        {
#if UNITY_2018_2_OR_NEWER
            var path = compiledAssemblyPaths.FirstOrDefault();
            if (!string.IsNullOrEmpty(path))
            {
                Directory.Delete(Path.GetDirectoryName(path), true);
            }
            compiledAssemblyPaths.Clear();
            var outputFolder = FileUtil.GetUniqueTempPathInProject();
            if (Directory.Exists(outputFolder))
                Directory.Delete(outputFolder, true);

            ScriptCompilationSettings input = new ScriptCompilationSettings();
            input.target = EditorUserBuildSettings.activeBuildTarget;
            input.@group = EditorUserBuildSettings.selectedBuildTargetGroup;

            var compilationResult = PlayerBuildInterface.CompilePlayerScripts(input, outputFolder);
            
            foreach (var assembly in compilationResult.assemblies)
            {
                compiledAssemblyPaths.Add(Path.Combine(outputFolder, assembly));    
            }

            return compilationResult.assemblies.Count > 0;
#else
            compiledAssemblyPaths.Clear();
            // fallback to CompilationPipeline assemblies 
            foreach (var playerAssembly in CompilationPipeline.GetAssemblies().Where(a => a.flags != AssemblyFlags.EditorAssembly))
            {
                compiledAssemblyPaths.Add(playerAssembly.outputPath);                   
            }   

            return true;
#endif
        }
        static public IEnumerable<string> GetCompiledAssemblyPaths()
        {
            return compiledAssemblyPaths;  
        }

        static public IEnumerable<string> GetCompiledAssemblyDirectories()
        {
            return GetCompiledAssemblyPaths().Select(path => Path.GetDirectoryName(path)).Distinct();
        }

        static public IEnumerable<string> GetPrecompiledAssemblyPaths()
        {
            var assemblyPaths = new List<string>();
#if UNITY_2019_1_OR_NEWER
            assemblyPaths.AddRange(CompilationPipeline.GetPrecompiledAssemblyPaths(CompilationPipeline.PrecompiledAssemblySources
                .UserAssembly));
            assemblyPaths.AddRange(CompilationPipeline.GetPrecompiledAssemblyPaths(CompilationPipeline.PrecompiledAssemblySources
                .UnityEngine));
#elif UNITY_2018_4_OR_NEWER 
            assemblyPaths.AddRange(CompilationPipeline.GetPrecompiledAssemblyNames().Select(a => CompilationPipeline.GetPrecompiledAssemblyPathFromAssemblyName(a)));
#endif
            return assemblyPaths;
        }

        static public IEnumerable<string> GetPrecompiledAssemblyDirectories()
        {
            foreach (var dir in GetPrecompiledAssemblyPaths().Select(path => Path.GetDirectoryName(path)).Distinct())
            {
                yield return dir;
            }
        }
        
        static public IEnumerable<string> GetPrecompiledEngineAssemblyPaths()
        {
            var assemblyPaths = new List<string>();
#if UNITY_2019_1_OR_NEWER
            assemblyPaths.AddRange(CompilationPipeline.GetPrecompiledAssemblyPaths(CompilationPipeline.PrecompiledAssemblySources
                .UnityEngine));
#else
            assemblyPaths.AddRange( Directory.GetFiles(Path.Combine(EditorApplication.applicationContentsPath, Path.Combine("Managed",
                "UnityEngine"))).Where(path => Path.GetExtension(path).Equals(".dll")));
#endif

#if !UNITY_2019_2_OR_NEWER
            var files = Directory.GetFiles(Path.Combine(EditorApplication.applicationContentsPath, Path.Combine("UnityExtensions",
                Path.Combine("Unity", "GUISystem"))));
            assemblyPaths.AddRange( files.Where(path => Path.GetExtension(path).Equals(".dll")));
#endif
            return assemblyPaths;
        }
        
        static public IEnumerable<string> GetPrecompiledEngineAssemblyDirectories()
        {
            foreach (var dir in GetPrecompiledEngineAssemblyPaths().Select(path => Path.GetDirectoryName(path)).Distinct())
            {
                yield return dir;
            }
        }
    }
}