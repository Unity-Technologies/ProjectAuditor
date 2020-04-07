using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;

#if UNITY_2019_3_OR_NEWER
using UnityEditor.PackageManager;
#endif

namespace Unity.ProjectAuditor.Editor.Utils
{
    public static class AssemblyHelper
    {
        public static string DefaultAssemblyFileName = "Assembly-CSharp.dll";

        public static string DefaultAssemblyName
        {
            get { return Path.GetFileNameWithoutExtension(DefaultAssemblyFileName); }
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
            assemblyPaths.AddRange(CompilationPipeline.GetPrecompiledAssemblyNames()
                .Select(a => CompilationPipeline.GetPrecompiledAssemblyPathFromAssemblyName(a)));
#endif
            return assemblyPaths;
        }

        public static IEnumerable<string> GetPrecompiledAssemblyDirectories()
        {
            foreach (var dir in GetPrecompiledAssemblyPaths().Select(path => Path.GetDirectoryName(path)).Distinct())
                yield return dir;
        }

        public static IEnumerable<string> GetPrecompiledEngineAssemblyPaths()
        {
            var assemblyPaths = new List<string>();
#if UNITY_2019_1_OR_NEWER
            assemblyPaths.AddRange(CompilationPipeline.GetPrecompiledAssemblyPaths(CompilationPipeline.PrecompiledAssemblySources
                .UnityEngine));
#else
            assemblyPaths.AddRange(Directory.GetFiles(Path.Combine(EditorApplication.applicationContentsPath,
                Path.Combine("Managed",
                    "UnityEngine"))).Where(path => Path.GetExtension(path).Equals(".dll")));
#endif

#if !UNITY_2019_2_OR_NEWER
            var files = Directory.GetFiles(Path.Combine(EditorApplication.applicationContentsPath, Path.Combine(
                "UnityExtensions",
                Path.Combine("Unity", "GUISystem"))));
            assemblyPaths.AddRange(files.Where(path => Path.GetExtension(path).Equals(".dll")));
#endif
            return assemblyPaths;
        }

        public static IEnumerable<string> GetPrecompiledEngineAssemblyDirectories()
        {
            foreach (var dir in GetPrecompiledEngineAssemblyPaths().Select(path => Path.GetDirectoryName(path))
                     .Distinct()) yield return dir;
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

        public static bool IsAssemblyFromReadOnlyPackage(string assemblyName)
        {
#if UNITY_2019_3_OR_NEWER
            var module = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.Modules).FirstOrDefault(a => a.Name.Contains(assemblyName));
            if (module == null)
                return false;
            var info =  UnityEditor.PackageManager.PackageInfo.FindForAssembly(module.Assembly);
            if (info == null)
                return false;
            return info.source != PackageSource.Embedded;
#else
            // assume it's not a package
            return false;
#endif
        }
    }
}
