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
        // this is required to generate Assembly-CSharp.dll
        TestAsset m_TestAsset = new TestAsset("MyClass.cs", @"
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
}");
#pragma warning restore 0414

        [Test]
        public void AssemblyCompilation_DefaultSettings_AreCorrect()
        {
            using (var compilationHelper = new AssemblyCompilation())
            {
                Assert.AreEqual(CompilationMode.Player, compilationHelper.CompilationMode);
                Assert.AreEqual(EditorUserBuildSettings.activeBuildTarget, compilationHelper.Platform);
            }
        }

        [Test]
        public void AssemblyCompilation_DefaultAssembly_IsCompiled()
        {
            using (var compilationHelper = new AssemblyCompilation())
            {
                var assemblyInfos = compilationHelper.Compile();

                Assert.Positive(assemblyInfos.Count());
                Assert.NotNull(assemblyInfos.FirstOrDefault(info => info.Name.Equals(AssemblyInfo.DefaultAssemblyName)));
            }
        }

        [Test]
        public void AssemblyCompilation_EditorAssembly_IsCompiled()
        {
            using (var compilationHelper = new AssemblyCompilation
               {
                   CompilationMode =  CompilationMode.Editor
               })
            {
                var assemblyInfo = compilationHelper.Compile().FirstOrDefault(a => a.Name.Equals("Unity.ProjectAuditor.Editor"));

                Assert.NotNull(assemblyInfo);
            }
        }

        [Test]
        public void AssemblyCompilation_EditorAssembly_IsNotCompiled()
        {
            using (var compilationHelper = new AssemblyCompilation
               {
                   CompilationMode =  CompilationMode.EditorPlayMode
               })
            {
                var assemblyInfo = compilationHelper.Compile().FirstOrDefault(a => a.Name.Equals("Unity.ProjectAuditor.Editor"));

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
            var projectAuditor = new Unity.ProjectAuditor.Editor.ProjectAuditor();
            var report = projectAuditor.Audit(new AnalysisParams
            {
                CompilationMode = mode
            });

            var issues = report.FindByCategory(IssueCategory.Code);
            var codeIssue = issues.FirstOrDefault(i => i.RelativePath.Equals(m_TestAsset.RelativePath));

            Assert.NotNull(codeIssue);
            Assert.AreEqual("MyClass." + methodName, codeIssue.Dependencies.PrettyName);
        }
    }
}
