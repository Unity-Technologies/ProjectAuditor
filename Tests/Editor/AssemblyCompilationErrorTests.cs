using System;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEngine;
using UnityEngine.TestTools;

namespace UnityEditor.ProjectAuditor.EditorTests
{
    public class AssemblyCompilationErrorTests
    {
#pragma warning disable 0414
        TempAsset m_TempAsset;
#pragma warning restore 0414

        [OneTimeSetUp]
        public void SetUp()
        {
            m_TempAsset = new TempAsset("MyClass.cs", @"
class MyClass {
#if !UNITY_EDITOR
    asd
#endif
}
");
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            TempAsset.Cleanup();
        }

        [Test]
        [ExplicitAttribute]
        public void CompilerMessageIsReported()
        {
            LogAssert.ignoreFailingMessages = true;

            using (var compilationHelper = new AssemblyCompilationPipeline
            {
                AssemblyCompilationFinished = (assemblyName, messages) =>
                {
                    Assert.True(assemblyName.Equals(AssemblyInfo.DefaultAssemblyName));
                }
            })
            {
                compilationHelper.Compile();
            }

            LogAssert.Expect(LogType.Error,
                "Assets/ProjectAuditor-Temp/MyClass.cs(6,1): error CS1519: Invalid token '}' in class, struct, or interface member declaration");
            LogAssert.Expect(LogType.Error, "Failed to compile player scripts");
            LogAssert.ignoreFailingMessages = false;
        }
    }
}
