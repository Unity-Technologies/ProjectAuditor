using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

#if UNITY_2019_3_OR_NEWER
using UnityEditor.PackageManager;
#endif

namespace Unity.ProjectAuditor.Editor.Utils
{
    // PrecompiledAssemblyType is a 1:1 match to PrecompiledAssemblySources (https://docs.unity3d.com/2019.4/Documentation/ScriptReference/Compilation.CompilationPipeline.PrecompiledAssemblySources.html)
    [Flags]
    enum PrecompiledAssemblyTypes
    {
        /// <summary>
        ///   <para>Matches precompiled assemblies present in the project and packages.</para>
        /// </summary>
        UserAssembly = 1,
        /// <summary>
        ///   <para>Matches UnityEngine and runtime module assemblies.</para>
        /// </summary>
        UnityEngine = 2,
        /// <summary>
        ///   <para>Matches UnityEditor and editor module assemblies.</para>
        /// </summary>
        UnityEditor = 4,
        /// <summary>
        ///   <para>Matches assemblies supplied by the target framework.</para>
        /// </summary>
        SystemAssembly = 8,
        /// <summary>
        ///   <para>Matches all assembly sources.</para>
        /// </summary>
        All = -1 // 0xFFFFFFFF
    }

    static class AssemblyInfoProvider
    {
        const string k_BuiltInPackagesFolder = "BuiltInPackages";

        internal static IEnumerable<string> GetPrecompiledAssemblyPaths(PrecompiledAssemblyTypes flags)
        {
            var assemblyPaths = new List<string>();
#if UNITY_2019_1_OR_NEWER
            var precompiledAssemblySources = (CompilationPipeline.PrecompiledAssemblySources)flags;
            assemblyPaths.AddRange(CompilationPipeline.GetPrecompiledAssemblyPaths(precompiledAssemblySources));
#else
            if ((flags & PrecompiledAssemblyTypes.UnityEngine) != 0)
                assemblyPaths.AddRange(Directory.GetFiles(Path.Combine(EditorApplication.applicationContentsPath,
                    Path.Combine("Managed", "UnityEngine"))).Where(path => Path.GetExtension(path).Equals(".dll")));
            if ((flags & PrecompiledAssemblyTypes.UnityEditor) != 0)
                assemblyPaths.AddRange(Directory.GetFiles(Path.Combine(EditorApplication.applicationContentsPath,
                    "Managed")).Where(path => Path.GetExtension(path).Equals(".dll")));
            if ((flags & PrecompiledAssemblyTypes.UserAssembly) != 0)
                assemblyPaths.AddRange(CompilationPipeline.GetPrecompiledAssemblyNames().Select(name => CompilationPipeline.GetPrecompiledAssemblyPathFromAssemblyName(name)));
#endif

#if !UNITY_2019_2_OR_NEWER
            var extensions = new List<string>();
            if ((flags & PrecompiledAssemblyTypes.UnityEngine) != 0)
            {
                extensions.AddRange(new[]
                {
                    "UnityExtensions/Unity/Networking/UnityEngine.Networking.dll",
                    "UnityExtensions/Unity/Timeline/Runtime/UnityEngine.Timeline.dll",
                    "UnityExtensions/Unity/GUISystem/UnityEngine.UI.dll",
                });
            }
            if ((flags & PrecompiledAssemblyTypes.UnityEditor) != 0)
            {
                extensions.AddRange(new[]
                {
                    "UnityExtensions/Unity/Networking/Editor/UnityEditor.Networking.dll",
                    "UnityExtensions/Unity/Timeline/Editor/UnityEditor.Timeline.dll",
                    "UnityExtensions/Unity/GUISystem/Editor/UnityEditor.UI.dll",
                });
            }
            assemblyPaths.AddRange(extensions.Select(ext => Path.Combine(EditorApplication.applicationContentsPath, ext)));
#endif
            return assemblyPaths.Select(path => path.Replace("\\", "/"));
        }

        public static IEnumerable<string> GetPrecompiledAssemblyDirectories(PrecompiledAssemblyTypes flags)
        {
            foreach (var dir in GetPrecompiledAssemblyPaths(flags).Select(Path.GetDirectoryName).Distinct())
                yield return dir;
        }

        public static bool IsUnityEngineAssembly(string assemblyName)
        {
            return GetPrecompiledAssemblyPaths(PrecompiledAssemblyTypes.UnityEngine).FirstOrDefault(a => a.Contains(assemblyName)) != null;
        }

        public static bool IsReadOnlyAssembly(string assemblyName)
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
                    return assemblyInfo;
                }
            }
            else if (!assemblyInfo.name.StartsWith(AssemblyInfo.DefaultAssemblyName))
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
