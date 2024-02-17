using System;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.AssemblyUtils;
using Unity.ProjectAuditor.Editor.Tests.Common;
using Unity.ProjectAuditor.Editor.UI.Framework;
using UnityEditor;
using UnityEngine;
using Assert = UnityEngine.Assertions.Assert;

namespace Unity.ProjectAuditor.EditorTests
{
    class TextFilterTests : TestFixtureBase
    {
#pragma warning disable 0414
        TestAsset m_TestAsset;
#pragma warning restore 0414

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            m_TestAsset = new TestAsset("FilterTests.cs", @"
using UnityEngine;

class WrapperClass
{
    InternalClass impl;
    void DoSomething()
    {
        impl.DoSomething();
    }
}

class InternalClass
{
    public void DoSomething()
    {
        Debug.Log(Camera.allCameras.Length.ToString());
    }
}
");
        }

        [Test]
        public void TextFilter_EmptyString_MatchesAll()
        {
            var stringFilter = new TextFilter
            {
                ignoreCase = false,
                searchDependencies = false,
                searchString = string.Empty
            };

            var issues = Analyze(IssueCategory.ProjectSetting);
            var filteredIssues = issues.Where(i => stringFilter.Match(i));

            Assert.AreEqual(issues.Length, filteredIssues.Count());
        }

        [Test]
        public void TextFilter_CaseSensitive_Matches()
        {
            var issues = Analyze(IssueCategory.ProjectSetting);
            var stringFilter = new TextFilter
            {
                ignoreCase = false,
                searchDependencies = false,
                searchString = "Prebake Collision Meshes"
            };
            var filteredIssues = issues.Where(i => stringFilter.Match(i));

            Assert.AreEqual(1, filteredIssues.Count());

            stringFilter.searchString = "prebake collision meshes";

            filteredIssues = issues.Where(i => stringFilter.Match(i));

            Assert.AreEqual(0, filteredIssues.Count());
        }

        [Test]
        public void TextFilter_CaseInsensitive_Matches()
        {
            var issues = Analyze(IssueCategory.ProjectSetting);
            var stringFilter = new TextFilter
            {
                ignoreCase = true,
                searchDependencies = false,
                searchString = "prebake collision meshes"
            };
            var filteredIssues = issues.Where(i => stringFilter.Match(i));
            Assert.AreEqual(1, filteredIssues.Count());
        }

        [Test]
        public void TextFilter_Filename_Matches()
        {
            var projectAuditor = new Unity.ProjectAuditor.Editor.ProjectAuditor();
            var report = projectAuditor.Audit(new AnalysisParams
            {
                CompilationMode = CompilationMode.Player
            });
            var issues = report.FindByCategory(IssueCategory.Code);
            var stringFilter = new TextFilter
            {
                ignoreCase = true,
                searchDependencies = false,
                searchString = "FilterTests.cs"
            };
            var filteredIssues = issues.Where(i => stringFilter.Match(i));
            Assert.IsTrue(filteredIssues.Count() >= 1);
        }

        [Test]
        public void TextFilter_RecursiveSearch_Matches()
        {
            var projectAuditor = new Unity.ProjectAuditor.Editor.ProjectAuditor();
            var report = projectAuditor.Audit(new AnalysisParams
            {
                CompilationMode = CompilationMode.Player
            });
            var issues = report.FindByCategory(IssueCategory.Code);
            var stringFilter = new TextFilter
            {
                ignoreCase = false,
                searchDependencies = false,
                searchString = "WrapperClass"
            };
            var filteredIssues = issues.Where(i => stringFilter.Match(i));
            Assert.AreEqual(0, filteredIssues.Count());

            // try again looking into dependencies too
            stringFilter.searchDependencies = true;

            filteredIssues = issues.Where(i => stringFilter.Match(i));
            Assert.IsTrue(filteredIssues.Count() >= 1);
        }
    }
}
