using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.AssemblyUtils;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace Unity.ProjectAuditor.EditorTests
{
    class AssemblyInfoTests
    {
#pragma warning disable 0414
        TempAsset m_TempAsset;
#pragma warning restore 0414

        [OneTimeSetUp]
        public void SetUp()
        {
            // this is required so the default assembly is generated when testing on an empty project (i.e: on Yamato)
            m_TempAsset = new TempAsset("MyClass.cs", "class MyClass { void MyMethod() { UnityEngine.Debug.Log(666); } }");
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            TempAsset.Cleanup();
        }

        [Test]
#if UNITY_2020_2_OR_NEWER
        [TestCase("/Managed/UnityEngine/UnityEditor.dll")]
        [TestCase("/Managed/UnityEngine/UnityEditor.CoreModule.dll")]
#else
        [TestCase("/Managed/UnityEditor.dll")]
#endif
        public void AssemblyInfo_UnityEditorAssemblyPath_IsFound(string assemblyRelativePath)
        {
            var paths = AssemblyInfoProvider.GetPrecompiledAssemblyPaths(PrecompiledAssemblyTypes.UnityEditor);
            var expectedPath = EditorApplication.applicationContentsPath + assemblyRelativePath;
            var result = paths.FirstOrDefault(path => path.Equals(expectedPath));

            Assert.NotNull(result);
        }

        [Test]
        [TestCase("/Managed/UnityEngine/UnityEngine.dll")]
        [TestCase("/Managed/UnityEngine/UnityEngine.CoreModule.dll")]
        public void AssemblyInfo_UnityEngineAssemblyPath_IsFound(string assemblyRelativePath)
        {
            var paths = AssemblyInfoProvider.GetPrecompiledAssemblyPaths(PrecompiledAssemblyTypes.UnityEngine);
            var expectedPath = EditorApplication.applicationContentsPath + assemblyRelativePath;
            var result = paths.FirstOrDefault(path => path.Equals(expectedPath));

            Assert.NotNull(result);
        }

#if !UNITY_2019_2_OR_NEWER
        [Test]
        [TestCase("UnityEditor.Networking.dll")]
        [TestCase("UnityEditor.UI.dll")]
        [TestCase("UnityEditor.Timeline.dll")]
        public void AssemblyInfo_UnityEditorExtensionAssemblyPath_IsFound(string assemblyName)
        {
            var paths = AssemblyInfoProvider.GetPrecompiledAssemblyPaths(PrecompiledAssemblyTypes.All);
            var result = paths.FirstOrDefault(path => path.EndsWith(assemblyName));

            Assert.NotNull(result);
        }

        [Test]
        [TestCase("UnityEngine.Networking.dll")]
        [TestCase("UnityEngine.UI.dll")]
        [TestCase("UnityEngine.Timeline.dll")]
        public void AssemblyInfo_UnityEngineExtensionAssemblyPath_IsFound(string assemblyName)
        {
            var paths = AssemblyInfoProvider.GetPrecompiledAssemblyPaths(PrecompiledAssemblyTypes.UnityEngine);
            var result = paths.FirstOrDefault(path => path.EndsWith(assemblyName));

            Assert.NotNull(result);
        }

#endif

        [Test]
        public void AssemblyInfo_PackageAssemblyPath_IsFound()
        {
            // check mono cecil is found
            var paths = AssemblyInfoProvider.GetPrecompiledAssemblyPaths(PrecompiledAssemblyTypes.UserAssembly);
            var result = paths.FirstOrDefault(path => path.EndsWith("Mono.Cecil.dll"));

            Assert.NotNull(result);
        }

        [Test]
        public void AssemblyInfo_AssetPaths_CanBeResolved()
        {
            var acceptablePrefixes = new[]
            {
#if !UNITY_2019_1_OR_NEWER
                "Library/PackageCache/",
#endif
                "Assets/",
                "Packages/",
                "Resources/unity_builtin_extra",
                "Unity.SourceGenerators/",
                PathUtils.Combine(AssemblyInfoProvider.s_ProjectPath, "Unity.SourceGenerators"),
                "Built-in",                        // prefix for built-in resources such as textures (not a real prefix path)
            };

            var issues = Utility.AnalyzeBuild(i => i.category != IssueCategory.ProjectSetting);
            foreach (var issue in issues)
            {
                var relativePath = issue.relativePath;
                Assert.True(string.IsNullOrEmpty(relativePath) || acceptablePrefixes.Any(prefix => relativePath.StartsWith(prefix)), "Path: " + relativePath + " Category: " + issue.category);
            }
        }

        [Test]
        public void AssemblyInfo_DefaultAssemblyPath_CanBeResolved()
        {
            var assembly = CompilationPipeline.GetAssemblies(AssembliesType.Player).FirstOrDefault(a => a.name.Equals(Path.GetFileNameWithoutExtension(AssemblyInfo.DefaultAssemblyFileName)));

            Assert.NotNull(assembly);

            var assemblyInfo = AssemblyInfoProvider.GetAssemblyInfoFromAssemblyPath(assembly.outputPath);
            var path = AssemblyInfoProvider.ResolveAssetPath(assemblyInfo, Path.Combine(Application.dataPath, "somefile"));

            Assert.AreEqual("Assets/somefile", path, "Resolved Path is: " + path);
        }

        [Test]
        public void AssemblyInfo_DefaultAssembly_IsCorrect()
        {
            var assembly = CompilationPipeline.GetAssemblies(AssembliesType.Player).FirstOrDefault(a => a.name.Equals(Path.GetFileNameWithoutExtension(AssemblyInfo.DefaultAssemblyFileName)));

            Assert.NotNull(assembly);

            var assemblyInfo = AssemblyInfoProvider.GetAssemblyInfoFromAssemblyPath(assembly.outputPath);

            Assert.AreEqual("Library/ScriptAssemblies/Assembly-CSharp.dll", assemblyInfo.path);
            Assert.IsNull(assemblyInfo.asmDefPath);
            Assert.IsFalse(assemblyInfo.packageReadOnly);
        }

        [Test]
        public void AssemblyInfo_LocalPackageAssemblyInfo_IsCorrect()
        {
            var assembly = CompilationPipeline.GetAssemblies(AssembliesType.Editor).FirstOrDefault(a => a.name.Equals("Unity.ProjectAuditor.Editor"));

            Assert.NotNull(assembly);

            var assemblyInfo = AssemblyInfoProvider.GetAssemblyInfoFromAssemblyPath(assembly.outputPath);

            Assert.AreEqual("Library/ScriptAssemblies/Unity.ProjectAuditor.Editor.dll", assemblyInfo.path);
            Assert.AreEqual(Unity.ProjectAuditor.Editor.ProjectAuditor.PackagePath + "/Editor/Unity.ProjectAuditor.Editor.asmdef", assemblyInfo.asmDefPath);
            Assert.AreEqual(Unity.ProjectAuditor.Editor.ProjectAuditor.PackagePath, assemblyInfo.relativePath);
        }

        [Test]
        [Ignore("Library\\PackageCache should only be used in 2018 so it's safe to ignore")]
        public void AssemblyInfo_PackageAssemblyPath_CanBeResolved()
        {
            var assembly = CompilationPipeline.GetAssemblies(AssembliesType.Player).FirstOrDefault(a => a.name.Contains("UnityEngine.UI"));
            var assemblyInfo = AssemblyInfoProvider.GetAssemblyInfoFromAssemblyPath(assembly.outputPath);
            var path = AssemblyInfoProvider.ResolveAssetPath(assemblyInfo, Path.Combine(Application.dataPath, "Library\\PackageCache\\com.unity.ugui@1.0.0\\Runtime\\UI\\Core\\AnimationTriggers.cs"));

            Assert.AreEqual("Packages/com.unity.ugui/Runtime/UI/Core/AnimationTriggers.cs", path, "Resolved Path is: " + path);
        }

#if UNITY_2019_1_OR_NEWER
        [Test]
        public void AssemblyInfo_RegistryPackageAssembly_IsReadOnly()
        {
            Assert.IsTrue(AssemblyInfoProvider.IsReadOnlyAssembly("UnityEngine.TestRunner"));
        }

#endif
    }
}
