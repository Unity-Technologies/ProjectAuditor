using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
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
            m_TempAsset = new TempAsset("MyClass.cs", "class MyClass { }");
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

#if UNITY_2018_1_OR_NEWER
        [Test]
        public void AssemblyInfo_DefaultAssembly_AssetPathCanBeResolved()
        {
            var assembly = CompilationPipeline.GetAssemblies(AssembliesType.Player).FirstOrDefault(a => a.name.Equals(Path.GetFileNameWithoutExtension(AssemblyInfo.DefaultAssemblyFileName)));
            var assemblyInfo = AssemblyInfoProvider.GetAssemblyInfoFromAssemblyPath(assembly.outputPath);

            var path = AssemblyInfoProvider.ResolveAssetPath(assemblyInfo, Path.Combine(Application.dataPath, "somefile"));

            Assert.True(path.Equals("Assets/somefile"));
        }

        [Test]
        public void AssemblyInfo_DefaultAssembly_IsCorrect()
        {
            var assembly = CompilationPipeline.GetAssemblies(AssembliesType.Player).FirstOrDefault(a => a.name.Equals(Path.GetFileNameWithoutExtension(AssemblyInfo.DefaultAssemblyFileName)));
            var assemblyInfo = AssemblyInfoProvider.GetAssemblyInfoFromAssemblyPath(assembly.outputPath);

            Assert.IsTrue(assemblyInfo.path.Equals("Library/ScriptAssemblies/Assembly-CSharp.dll"));
            Assert.IsNull(assemblyInfo.asmDefPath);
            Assert.IsFalse(assemblyInfo.readOnly);
        }

        [Test]
        public void AssemblyInfo_LocalPackageAssemblyInfo_IsCorrect()
        {
            var assembly = CompilationPipeline.GetAssemblies(AssembliesType.Editor).FirstOrDefault(a => a.name.Equals("Unity.ProjectAuditor.Editor"));
            var assemblyInfo = AssemblyInfoProvider.GetAssemblyInfoFromAssemblyPath(assembly.outputPath);

            Assert.IsTrue(assemblyInfo.path.Equals("Library/ScriptAssemblies/Unity.ProjectAuditor.Editor.dll"));
            Assert.IsTrue(assemblyInfo.asmDefPath.Equals(Unity.ProjectAuditor.Editor.ProjectAuditor.PackagePath + "/Editor/Unity.ProjectAuditor.Editor.asmdef"));
            Assert.IsTrue(assemblyInfo.relativePath.Equals(Unity.ProjectAuditor.Editor.ProjectAuditor.PackagePath));
        }

#endif

#if UNITY_2019_1_OR_NEWER
        [Test]
        public void AssemblyInfo_RegistryPackageAssembly_IsReadOnly()
        {
            Assert.IsTrue(AssemblyInfoProvider.IsReadOnlyAssembly("UnityEngine.TestRunner"));
        }

#endif
    }
}
