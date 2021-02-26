using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Auditors;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor.Compilation;
using UnityEngine;

namespace UnityEditor.ProjectAuditor.EditorTests
{
    public class AssemblyHelperTests
    {
#pragma warning disable 0414
        TempAsset m_TempAsset;
#pragma warning restore 0414

        [OneTimeSetUp]
        public void SetUp()
        {
            // this is required so the default assembly is generated
            m_TempAsset = new TempAsset("MyClass.cs", "class MyClass { }");
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            TempAsset.Cleanup();
        }

        [Test]
        public void CanGetBuiltinAuditorTypes()
        {
            var types = AssemblyHelper.GetAllTypesInheritedFromInterface<IAuditor>();

            Assert.NotNull(types.FirstOrDefault(type => type == typeof(ScriptAuditor)));
            Assert.NotNull(types.FirstOrDefault(type => type == typeof(SettingsAuditor)));
        }

        [Test]
        public void DefaultAssemblyPathIsFound()
        {
            using (var compilationHelper = new AssemblyCompilationHelper())
            {
                var assemblyInfos = compilationHelper.Compile();

                Assert.Positive(assemblyInfos.Count());
                Assert.NotNull(assemblyInfos.FirstOrDefault(info => info.name.Contains(AssemblyHelper.DefaultAssemblyName)));
            }
        }

        [Test]
        public void UnityEngineModuleAssemblyPathIsFound()
        {
            var paths = AssemblyHelper.GetPrecompiledEngineAssemblyPaths();

            Assert.Positive(paths.Count());

            var expectedPath = EditorApplication.applicationContentsPath + "/Managed/UnityEngine/UnityEngine.CoreModule.dll";
            var result = paths.FirstOrDefault(path => path.Equals(expectedPath));
            Assert.NotNull(result);
        }

#if UNITY_2018_1_OR_NEWER
        [Test]
        public void CanResolveDefaultAssemblyAssetPath()
        {
            var assembly = CompilationPipeline.GetAssemblies(AssembliesType.Player).FirstOrDefault(a => a.name.Equals(Path.GetFileNameWithoutExtension(AssemblyHelper.DefaultAssemblyFileName)));
            var assemblyInfo = AssemblyHelper.GetAssemblyInfoFromAssemblyPath(assembly.outputPath);

            var path = AssemblyHelper.ResolveAssetPath(assemblyInfo, Path.Combine(Application.dataPath, "somefile"));

            Assert.True(path.Equals("Assets/somefile"));
        }

        [Test]
        public void DefaultAssemblyInfoIsCorrect()
        {
            var assembly = CompilationPipeline.GetAssemblies(AssembliesType.Player).FirstOrDefault(a => a.name.Equals(Path.GetFileNameWithoutExtension(AssemblyHelper.DefaultAssemblyFileName)));
            var assemblyInfo = AssemblyHelper.GetAssemblyInfoFromAssemblyPath(assembly.outputPath);

            Assert.IsTrue(assemblyInfo.path.Equals("Library/ScriptAssemblies/Assembly-CSharp.dll"));
            Assert.IsNull(assemblyInfo.asmDefPath);
            Assert.IsFalse(assemblyInfo.readOnly);
        }

        [Test]
        public void PackageAssemblyInfoIsCorrect()
        {
            var assembly = CompilationPipeline.GetAssemblies(AssembliesType.Editor).FirstOrDefault(a => a.name.Equals("Unity.ProjectAuditor.Editor"));
            var assemblyInfo = AssemblyHelper.GetAssemblyInfoFromAssemblyPath(assembly.outputPath);

            Assert.IsTrue(assemblyInfo.path.Equals("Library/ScriptAssemblies/Unity.ProjectAuditor.Editor.dll"));
            Assert.IsTrue(assemblyInfo.asmDefPath.Equals("Packages/com.unity.project-auditor/Editor/Unity.ProjectAuditor.Editor.asmdef"));
            Assert.IsTrue(assemblyInfo.relativePath.Equals("Packages/com.unity.project-auditor"));
        }

#endif

#if UNITY_2019_1_OR_NEWER
        [Test]
        public void RegistryPackageAssemblyIsReadOnly()
        {
            Assert.IsTrue(AssemblyHelper.IsAssemblyReadOnly("UnityEngine.TestRunner"));
        }

#endif
    }
}
