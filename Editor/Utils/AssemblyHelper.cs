using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

#if UNITY_2019_3_OR_NEWER
using UnityEditor.PackageManager;
#endif

namespace Unity.ProjectAuditor.Editor.Utils
{
    static class AssemblyHelper
    {
        const string k_BuiltInPackagesFolder = "BuiltInPackages";

        public const string DefaultAssemblyFileName = "Assembly-CSharp.dll";
        public static string DefaultAssemblyName
        {
            get { return Path.GetFileNameWithoutExtension(DefaultAssemblyFileName); }
        }

        static List<Type> s_Types;

        static IEnumerable<Type> GetAllTypes()
        {
            if (s_Types != null)
                return s_Types;

            var types = new List<Type>();
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    types.AddRange(a.GetTypes());
                }
                catch (ReflectionTypeLoadException /* e */)
                {
                    Debug.LogWarningFormat("Project Auditor: Could not get {0} types information", a.GetName().Name);
                }
            }

            s_Types = types;

            return types;
        }

        public static IEnumerable<Type> GetAllTypesInheritedFromInterface<InterfaceT>()
        {
            var interfaceType = typeof(InterfaceT);
            return GetAllTypes()
                .Where(type => type != interfaceType && interfaceType.IsAssignableFrom(type));
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
            return assemblyPaths.Select(path => path.Replace("\\", "/"));
        }

        public static IEnumerable<string> GetPrecompiledEngineAssemblyDirectories()
        {
            foreach (var dir in GetPrecompiledEngineAssemblyPaths().Select(path => Path.GetDirectoryName(path))
                     .Distinct()) yield return dir;
        }

        public static bool IsModuleAssembly(string assemblyName)
        {
            return GetPrecompiledEngineAssemblyPaths().FirstOrDefault(a => a.Contains(assemblyName)) != null;
        }

        public static bool IsAssemblyReadOnly(string assemblyName)
        {
            var info = GetAssemblyInfoFromAssemblyName(assemblyName);
            return info.readOnly;
        }

        public static AssemblyInfo GetAssemblyInfoFromAssemblyPath(string assemblyPath)
        {
            var info = GetAssemblyInfoFromAssemblyName(Path.GetFileNameWithoutExtension(assemblyPath));
            info.path = assemblyPath;
            return info;
        }

        static AssemblyInfo GetAssemblyInfoFromAssemblyName(string assemblyName)
        {
            // by default let's assume it's not a package
            var assemblyInfo = new AssemblyInfo
            {
                name = assemblyName,
                relativePath = "Assets",
                readOnly = false
            };

            var asmDefPath = CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(assemblyInfo.name);
            if (asmDefPath != null)
            {
                assemblyInfo.asmDefPath = asmDefPath;
                var folders = asmDefPath.Split('/');
                if (folders.Length > 2 && folders[0].Equals("Packages"))
                {
                    assemblyInfo.relativePath = Path.Combine(folders[0], folders[1]).Replace("\\", "/");
#if UNITY_2019_3_OR_NEWER
                    var info =  UnityEditor.PackageManager.PackageInfo.FindForAssetPath(asmDefPath);
                    if (info != null)
                    {
                        assemblyInfo.readOnly = info.source != PackageSource.Embedded && info.source != PackageSource.Local;
                    }
#else
                    assemblyInfo.readOnly = true;
#endif
                }
                else
                {
                    // non-package user-defined assembly
                }
            }
            else if (!assemblyInfo.name.StartsWith(DefaultAssemblyName))
            {
                Debug.LogErrorFormat("Assembly Definition cannot be found for " + assemblyInfo.name);
            }

            return assemblyInfo;
        }

        public static string ResolveAssetPath(AssemblyInfo assemblyInfo, string path)
        {
            // sanitize path
            path = path.Replace("\\", "/");

            if (!string.IsNullOrEmpty(assemblyInfo.asmDefPath) && assemblyInfo.asmDefPath.StartsWith("Packages"))
            {
                var asmDefFolder = Path.GetDirectoryName(assemblyInfo.asmDefPath.Remove(0, assemblyInfo.relativePath.Length + 1));
                if (path.IndexOf(asmDefFolder) < 0)
                {
                    // handle source files that are not located with asmdef
                    return Path.Combine(assemblyInfo.relativePath, assemblyInfo.sourcePaths.First(sourcePath => path.Contains(sourcePath)));
                }
                return Path.Combine(assemblyInfo.relativePath, path.Substring(path.IndexOf(asmDefFolder)));
            }
            if (path.Contains(k_BuiltInPackagesFolder))
            {
                return path.Remove(0, path.IndexOf(k_BuiltInPackagesFolder) + k_BuiltInPackagesFolder.Length);
            }

            // remove Assets folder
            var projectPath = Path.GetDirectoryName(Application.dataPath);
            return path.Remove(0, projectPath.Length + 1);
        }
    }
}
