using System;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.AssemblyUtils;
using UnityEditor;
using UnityEngine;
using Assert = UnityEngine.Assertions.Assert;

namespace Unity.ProjectAuditor.EditorTests
{
    class TextFilterTests
    {
#pragma warning disable 0414
        TempAsset m_TempAsset;
#pragma warning restore 0414

        bool m_PrevStripEngineCode;

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
            // disabling stripEngineCode will be reported as an issue
            m_PrevStripEngineCode = PlayerSettings.stripEngineCode;
            PlayerSettings.stripEngineCode = false;
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            TempAsset.Cleanup();

            // restore stripEngineCode
            PlayerSettings.stripEngineCode = m_PrevStripEngineCode;
        }

        [Test]
        public void TextFilter_EmptyString_MatchesAll()
        {
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
        public void TextFilter_CaseSensitive_Matches()
        {
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
        public void TextFilter_CaseInsensitive_Matches()
        {
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
        public void TextFilter_Filename_Matches()
        {
            var config = ScriptableObject.CreateInstance<ProjectAuditorConfig>();
            config.CompilationMode = CompilationMode.Player;

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
        public void TextFilter_RecursiveSearch_Matches()
        {
            var config = ScriptableObject.CreateInstance<ProjectAuditorConfig>();
            config.CompilationMode = CompilationMode.Player;

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
