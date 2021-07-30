using System;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using UnityEngine;
using Assert = UnityEngine.Assertions.Assert;

namespace UnityEditor.ProjectAuditor.EditorTests
{
    class FilterTests
    {
#pragma warning disable 0414
        TempAsset m_TempAsset;
#pragma warning restore 0414

        [OneTimeSetUp]
        public void SetUp()
        {
            m_TempAsset = new TempAsset("FilterTests.cs", @"
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

        [OneTimeTearDown]
        public void TearDown()
        {
            TempAsset.Cleanup();
        }

        [Test]
        public void EmptyStringMatchesAllIssues()
        {
            // disabling stripEngineCode will be reported as an issue
            PlayerSettings.stripEngineCode = false;

            var stringFilter = new TextFilter
            {
                ignoreCase = false,
                searchDependencies = false,
                searchText = string.Empty
            };

            var issues = Utility.Analyze(IssueCategory.ProjectSetting);
            var filteredIssues = issues.Where(i => stringFilter.Match(i));

            Assert.AreEqual(issues.Length, filteredIssues.Count());
        }

        [Test]
        public void CaseSensitiveMatch()
        {
            // disabling stripEngineCode will be reported as an issue
            PlayerSettings.stripEngineCode = false;

            var issues = Utility.Analyze(IssueCategory.ProjectSetting);
            var stringFilter = new TextFilter
            {
                ignoreCase = false,
                searchDependencies = false,
                searchText = "Engine Code Stripping"
            };
            var filteredIssues = issues.Where(i => stringFilter.Match(i));

            Assert.AreEqual(1, filteredIssues.Count());

            stringFilter.searchText = "engine code stripping";

            filteredIssues = issues.Where(i => stringFilter.Match(i));

            Assert.AreEqual(0, filteredIssues.Count());
        }

        [Test]
        public void CaseInsensitiveMatch()
        {
            // disabling stripEngineCode will be reported as an issue
            PlayerSettings.stripEngineCode = false;

            var issues = Utility.Analyze(IssueCategory.ProjectSetting);
            var stringFilter = new TextFilter
            {
                ignoreCase = true,
                searchDependencies = false,
                searchText = "engine code stripping"
            };
            var filteredIssues = issues.Where(i => stringFilter.Match(i));
            Assert.AreEqual(1, filteredIssues.Count());
        }

        [Test]
        public void FilenameMatch()
        {
            var config = ScriptableObject.CreateInstance<ProjectAuditorConfig>();
            config.AnalyzeEditorCode = false;

            var projectAuditor = new Unity.ProjectAuditor.Editor.ProjectAuditor(config);

            var projectReport = projectAuditor.Audit();
            var issues = projectReport.GetIssues(IssueCategory.Code);
            var stringFilter = new TextFilter
            {
                ignoreCase = true,
                searchDependencies = false,
                searchText = "FilterTests.cs"
            };
            var filteredIssues = issues.Where(i => stringFilter.Match(i));
            Assert.AreEqual(1, filteredIssues.Count());
        }

        [Test]
        public void RecursiveSearchMatch()
        {
            var config = ScriptableObject.CreateInstance<ProjectAuditorConfig>();
            config.AnalyzeEditorCode = false;

            var projectAuditor = new Unity.ProjectAuditor.Editor.ProjectAuditor(config);

            var projectReport = projectAuditor.Audit();
            var issues = projectReport.GetIssues(IssueCategory.Code);
            var stringFilter = new TextFilter
            {
                ignoreCase = false,
                searchDependencies = false,
                searchText = "WrapperClass"
            };
            var filteredIssues = issues.Where(i => stringFilter.Match(i));
            Assert.AreEqual(0, filteredIssues.Count());

            // try again looking into dependencies too
            stringFilter.searchDependencies = true;

            filteredIssues = issues.Where(i => stringFilter.Match(i));
            Assert.AreEqual(1, filteredIssues.Count());
        }
    }
}
