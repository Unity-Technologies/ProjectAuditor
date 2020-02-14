using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;

#if UNITY_2018_2_OR_NEWER
using UnityEditor.Build.Player;
#endif

#if UNITY_2019_3_OR_NEWER
using UnityEditor.PackageManager;
#endif

namespace Unity.ProjectAuditor.Editor.Utils
{
    public static class AssemblyHelper
    {
        private static string[] compiledAssemblyPaths = new string[]{};

        public static string DefaultAssemblyFileName = "Assembly-CSharp.dll";
        
        public static string DefaultAssemblyName
        {
            get
            {
                return Path.GetFileNameWithoutExtension(DefaultAssemblyFileName);
            }
        }
        
        public static bool CompileAssemblies()
        {
#if UNITY_2018_2_OR_NEWER
            var path = compiledAssemblyPaths.FirstOrDefault();
            if (!string.IsNullOrEmpty(path))
            {
                Directory.Delete(Path.GetDirectoryName(path), true);
            }

            var outputFolder = FileUtil.GetUniqueTempPathInProject();
            if (Directory.Exists(outputFolder))
                Directory.Delete(outputFolder, true);

            var input = new ScriptCompilationSettings
            {
                target = EditorUserBuildSettings.activeBuildTarget,
                @group = EditorUserBuildSettings.selectedBuildTargetGroup
            };

            var compilationResult = PlayerBuildInterface.CompilePlayerScripts(input, outputFolder);

            compiledAssemblyPaths =
                compilationResult.assemblies.Select(assembly => Path.Combine(outputFolder, assembly)).ToArray();

            return compilationResult.assemblies.Count > 0;
#else
            // fallback to CompilationPipeline assemblies 
            compiledAssemblyPaths = CompilationPipeline.GetAssemblies()
                .Where(a => a.flags != AssemblyFlags.EditorAssembly).Select(assembly => assembly.outputPath).ToArray();

            return true;
#endif
        }
        public static IEnumerable<string> GetCompiledAssemblyPaths()
        {
            return compiledAssemblyPaths;  
        }

        public static IEnumerable<string> GetCompiledAssemblyNames()
        {
            var list = compiledAssemblyPaths.Select(assemblyPath => Path.GetFileNameWithoutExtension(assemblyPath)).ToList();
            list.Sort();            
            return list.ToArray();
        }

        public static IEnumerable<string> GetCompiledAssemblyDirectories()
        {
            return GetCompiledAssemblyPaths().Select(path => Path.GetDirectoryName(path)).Distinct();
        }

        public static IEnumerable<string> GetPrecompiledAssemblyPaths()
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

        public static IEnumerable<string> GetPrecompiledAssemblyDirectories()
        {
            foreach (var dir in GetPrecompiledAssemblyPaths().Select(path => Path.GetDirectoryName(path)).Distinct())
            {
                yield return dir;
            }
        }
        
        public static IEnumerable<string> GetPrecompiledEngineAssemblyPaths()
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
        
        public static IEnumerable<string> GetPrecompiledEngineAssemblyDirectories()
        {
            foreach (var dir in GetPrecompiledEngineAssemblyPaths().Select(path => Path.GetDirectoryName(path)).Distinct())
            {
                yield return dir;
            }
        }

        public static bool IsPackageInfoAvailable()
        {
#if UNITY_2019_3_OR_NEWER
            return true;
#else
            return false;
#endif
        }

        public static bool IsModuleAssembly(string assemblyName)
        {
            return GetPrecompiledEngineAssemblyPaths().FirstOrDefault(a => a.Contains(assemblyName)) != null;
        }

        public static bool IsPackageAssembly(string assemblyName)
        {
#if UNITY_2019_3_OR_NEWER
            var module = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.Modules).FirstOrDefault(a => a.Name.Contains(assemblyName));
            return UnityEditor.PackageManager.PackageInfo.FindForAssembly(module.Assembly) != null;
#else
            // assume it's not a package
            return false;
#endif
        }
    }
}