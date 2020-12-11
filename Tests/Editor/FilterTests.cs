using System;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using UnityEngine;
using Assert = UnityEngine.Assertions.Assert;

namespace UnityEditor.ProjectAuditor.EditorTests
{
    public class FilterTests
    {
        TempAsset m_TempAsset;

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
        // Accessing Camera.main property is not recommended and will be reported as a possible performance problem.
        Debug.Log(Camera.main.name);
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
            var projectAuditor = new Unity.ProjectAuditor.Editor.ProjectAuditor();

            // disabling stripEngineCode will be reported as an issue
            PlayerSettings.stripEngineCode = false;

            var projectReport = projectAuditor.Audit();

            var stringFilter = new TextFilter
            {
                matchCase = true,
                searchDependencies = false,
                searchText = string.Empty
            };

            var issues = projectReport.GetIssues(IssueCategory.ProjectSettings);
            var filteredIssues = issues.Where(i => stringFilter.Match(i));

            Assert.AreEqual(issues.Length, filteredIssues.Count());
        }

        [Test]
        public void CaseSensitiveMatch()
        {
            var projectAuditor = new Unity.ProjectAuditor.Editor.ProjectAuditor();

            // disabling stripEngineCode will be reported as an issue
            PlayerSettings.stripEngineCode = false;

            var projectReport = projectAuditor.Audit();
            var issues = projectReport.GetIssues(IssueCategory.ProjectSettings);
            var stringFilter = new TextFilter
            {
                matchCase = true,
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
            var projectAuditor = new Unity.ProjectAuditor.Editor.ProjectAuditor();

            // disabling stripEngineCode will be reported as an issue
            PlayerSettings.stripEngineCode = false;

            var projectReport = projectAuditor.Audit();
            var issues = projectReport.GetIssues(IssueCategory.ProjectSettings);
            var stringFilter = new TextFilter
            {
                matchCase = false,
                searchDependencies = false,
                searchText = "engine code stripping"
            };
            var filteredIssues = issues.Where(i => stringFilter.Match(i));
            Assert.AreEqual(1, filteredIssues.Count());
        }

        [Test]
        public void FilenameMatch()
        {
            var projectAuditor = new Unity.ProjectAuditor.Editor.ProjectAuditor();

            var projectReport = projectAuditor.Audit();
            var issues = projectReport.GetIssues(IssueCategory.Code);
            var stringFilter = new TextFilter
            {
                matchCase = false,
                searchDependencies = false,
                searchText = "FilterTests.cs"
            };
            var filteredIssues = issues.Where(i => stringFilter.Match(i));
            Assert.AreEqual(1, filteredIssues.Count());
        }

        [Test]
        public void RecursiveSearchMatch()
        {
            var projectAuditor = new Unity.ProjectAuditor.Editor.ProjectAuditor();

            var projectReport = projectAuditor.Audit();
            var issues = projectReport.GetIssues(IssueCategory.Code);
            var stringFilter = new TextFilter
            {
                matchCase = true,
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
