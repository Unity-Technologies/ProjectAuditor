using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.AssemblyUtils;
using Unity.ProjectAuditor.Editor.Tests.Common;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace Unity.ProjectAuditor.EditorTests
{
    class AssemblyInfoTests : TestFixtureBase
    {
#pragma warning disable 0414
        // this is required so the default assembly is generated when testing on an empty project (i.e: on Yamato)
        TestAsset m_TestAsset = new TestAsset("MyClass.cs", "class MyClass { void MyMethod() { UnityEngine.Debug.Log(666); } }");
#pragma warning restore 0414

        [Test]
        [TestCase("/Managed/UnityEngine/UnityEditor.dll")]
        [TestCase("/Managed/UnityEngine/UnityEditor.CoreModule.dll")]
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
                "Assets/",
                "Packages/",
                "Resources/unity_builtin_extra",
                "Unity.SourceGenerators/",
                PathUtils.Combine(Editor.ProjectAuditor.ProjectPath, "Unity.SourceGenerators"),
                PathUtils.Combine(Editor.ProjectAuditor.ProjectPath, "Unity.Entities.SourceGen"),
                "Built-in",                        // prefix for built-in resources such as textures (not a real prefix path)
            };

            var issues = AnalyzeBuild(i => i.Category != IssueCategory.ProjectSetting && i.Category != IssueCategory.PrecompiledAssembly);
            foreach (var issue in issues)
            {
                var relativePath = issue.RelativePath;
                Assert.True(string.IsNullOrEmpty(relativePath) || acceptablePrefixes.Any(prefix => relativePath.StartsWith(prefix)), "Path: " + relativePath + " Category: " + issue.Category);
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

            Assert.AreEqual($"Library/ScriptAssemblies/{AssemblyInfo.DefaultAssemblyFileName}", assemblyInfo.Path);
            Assert.AreEqual("Built-in", assemblyInfo.AsmDefPath);
            Assert.IsFalse(assemblyInfo.IsPackageReadOnly);
        }

        [Test]
        public void AssemblyInfo_LocalPackageAssemblyInfo_IsCorrect()
        {
            var assembly = CompilationPipeline.GetAssemblies(AssembliesType.Editor).FirstOrDefault(a => a.name.Equals("Unity.ProjectAuditor.Editor"));

            Assert.NotNull(assembly);

            var assemblyInfo = AssemblyInfoProvider.GetAssemblyInfoFromAssemblyPath(assembly.outputPath);

            Assert.AreEqual("Library/ScriptAssemblies/Unity.ProjectAuditor.Editor.dll", assemblyInfo.Path);
            Assert.AreEqual(ProjectAuditorPackage.Path + "/Editor/Unity.ProjectAuditor.Editor.asmdef", assemblyInfo.AsmDefPath);
            Assert.AreEqual(ProjectAuditorPackage.Path, assemblyInfo.RelativePath);
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

        [Test]
        public void AssemblyInfo_RegistryPackageAssembly_IsReadOnly()
        {
            Assert.IsTrue(AssemblyInfoProvider.IsReadOnlyAssembly("UnityEngine.TestRunner"));
        }
    }
}
