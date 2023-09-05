using System;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.AssemblyUtils;
using Unity.ProjectAuditor.Editor.Tests.Common;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.EditorTests
{
    class AssemblyCompilationTests : TestFixtureBase
    {
#pragma warning disable 0414
        TestAsset m_TestAsset; // this is required to generate Assembly-CSharp.dll
#pragma warning restore 0414

        [OneTimeSetUp]
        public void SetUp()
        {
            m_TestAsset = new TestAsset("MyClass.cs", @"
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

        [Test]
        public void AssemblyCompilation_DefaultSettings_AreCorrect()
        {
            using (var compilationHelper = new AssemblyCompilation())
            {
                Assert.AreEqual(CompilationMode.Player, compilationHelper.compilationMode);
                Assert.AreEqual(EditorUserBuildSettings.activeBuildTarget, compilationHelper.platform);
            }
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
        public void AssemblyCompilation_EditorAssembly_IsCompiled()
        {
            using (var compilationHelper = new AssemblyCompilation
               {
                   compilationMode =  CompilationMode.Editor
               })
            {
                var assemblyInfo = compilationHelper.Compile().FirstOrDefault(a => a.name.Equals("Unity.ProjectAuditor.Editor"));

                Assert.NotNull(assemblyInfo);
            }
        }

        [Test]
        public void AssemblyCompilation_EditorAssembly_IsNotCompiled()
        {
            using (var compilationHelper = new AssemblyCompilation
               {
                   compilationMode =  CompilationMode.EditorPlayMode
               })
            {
                var assemblyInfo = compilationHelper.Compile().FirstOrDefault(a => a.name.Equals("Unity.ProjectAuditor.Editor"));

                Assert.Null(assemblyInfo);
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
            var projectAuditor = new Unity.ProjectAuditor.Editor.ProjectAuditor(config);
            var projectReport = projectAuditor.Audit(new ProjectAuditorParams
            {
                compilationMode = mode
            });

            var issues = projectReport.FindByCategory(IssueCategory.Code);
            var codeIssue = issues.FirstOrDefault(i => i.relativePath.Equals(m_TestAsset.relativePath));

            Assert.NotNull(codeIssue);
            Assert.AreEqual("MyClass." + methodName, codeIssue.dependencies.prettyName);
        }
    }
}
