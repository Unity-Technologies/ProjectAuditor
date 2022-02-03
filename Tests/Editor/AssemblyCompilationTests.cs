using System;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEngine;

namespace Unity.ProjectAuditor.EditorTests
{
    class AssemblyCompilationTests
    {
        TempAsset m_TempAsset; // this is required to generate Assembly-CSharp.dll

        [OneTimeSetUp]
        public void SetUp()
        {
            m_TempAsset = new TempAsset("MyClass.cs", @"
class MyClass
{
}
");
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            TempAsset.Cleanup();
        }

        [Test]
        public void AssemblyCompilation_DefaultAssembly_IsCompiled()
        {
            using (var compilationHelper = new AssemblyCompilationPipeline())
            {
                var assemblyInfos = compilationHelper.Compile();

                Assert.Positive(assemblyInfos.Count());
                Assert.NotNull(assemblyInfos.FirstOrDefault(info => info.name.Equals(AssemblyInfo.DefaultAssemblyName)));
            }
        }
    }
}
