using System;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.AssemblyUtils;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEngine;

namespace Unity.ProjectAuditor.EditorTests
{
    class AssemblyCompilationTests
    {
#pragma warning disable 0414
        TempAsset m_TempAsset; // this is required to generate Assembly-CSharp.dll
#pragma warning restore 0414

        [OneTimeSetUp]
        public void SetUp()
        {
            m_TempAsset = new TempAsset("MyClass.cs", @"
class MyClass
{
    object myObj;
#if UNITY_EDITOR
    void EditorMethod()
    { myObj = 9; }
#elif DEVELOPMENT_BUILD
    void DevelopmentPlayerMethod()
    { myObj = 9; }
#else
    void PlayerMethod()
    { myObj = 9; }
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
        public void AssemblyCompilation_DefaultAssembly_IsCompiled()
        {
            using (var compilationHelper = new AssemblyCompilation())
            {
                var assemblyInfos = compilationHelper.Compile();

                Assert.Positive(assemblyInfos.Count());
                Assert.NotNull(assemblyInfos.FirstOrDefault(info => info.name.Equals(AssemblyInfo.DefaultAssemblyName)));
            }
        }

        [Test]
        [TestCase(CompilationMode.Player, "PlayerMethod")]
        [TestCase(CompilationMode.DevelopmentPlayer, "DevelopmentPlayerMethod")]
        //Known failure because the script is not recompiled by the editor
        //[TestCase(CompilationMode.Editor, "Editor")]
        public void AssemblyCompilation_Player_IsCompiled(CompilationMode mode, string methodName)
        {
            var config = ScriptableObject.CreateInstance<ProjectAuditorConfig>();
            config.CompilationMode = mode;

            var projectAuditor = new Unity.ProjectAuditor.Editor.ProjectAuditor(config);
            var projectReport = projectAuditor.Audit();

            var issues = projectReport.GetIssues(IssueCategory.Code);
            var codeIssue = issues.FirstOrDefault(i => i.relativePath.Equals(m_TempAsset.relativePath));

            Assert.NotNull(codeIssue);
            Assert.AreEqual("MyClass." + methodName, codeIssue.dependencies.prettyName);
        }
    }
}
