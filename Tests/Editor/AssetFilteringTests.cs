using System;
using System.Diagnostics.CodeAnalysis;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Tests.Common;

namespace Unity.ProjectAuditor.EditorTests
{
    [SuppressMessage("ReSharper", "NotAccessedField.Local")]
    class AssetFilteringTests : TestFixtureBase
    {
        const string k_SmallMeshName = "SmallTestMesh";
        const string k_LargeMeshName = "LargeTestMesh";

        readonly IssueCategory[] m_AllCategoriesTested = new[]
        {
            IssueCategory.AssetIssue,
            IssueCategory.Code,
        };

        TestAsset m_CodeAsset;
        TestAsset m_SmallMeshAsset;
        TestAsset m_LargeMeshAsset;

        bool m_NullTestPassed = false;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            // this is required so the default assembly is generated when testing on an empty project (i.e: on Yamato)
            m_CodeAsset = new TestAsset("LinqError.cs",
                "using System.Linq;\n" +
                "class MyClass { void MyMethod() { UnityEngine.Debug.Log(666); } }"
            );

            var smallMesh = MeshGeneratorUtil.CreateTestMesh(k_SmallMeshName, 100);
            m_SmallMeshAsset = TestAsset.Save(smallMesh, k_SmallMeshName + ".mesh");

            var largeMesh = MeshGeneratorUtil.CreateTestMesh(k_LargeMeshName, 100000);
            m_LargeMeshAsset = TestAsset.Save(largeMesh, k_LargeMeshName + ".mesh");
        }

        [Test, Order(0)]
        public void AssetFiltering_NullSafety()
        {
            try
            {
                // ensure nothing else is going to cause a NullReferenceException
                AnalyzeFiltered(_ => true, m_AllCategoriesTested);
            }
            catch (NullReferenceException)
            {
                Assert.Inconclusive();
            }

            try
            {
                AnalyzeFiltered(null, m_AllCategoriesTested);
            }
            catch (NullReferenceException e)
            {
                Assert.Fail(e.StackTrace);
            }

            m_NullTestPassed = true;
        }

        [Test]
        public void AssetFiltering_IgnoredByCode()
        {
            if (!m_NullTestPassed)
            {
                Assert.Inconclusive();
            }

            var issuesAll = AnalyzeFiltered(null, IssueCategory.Code);
            var issuesNone = AnalyzeFiltered(_ => false, IssueCategory.Code);
            Assert.NotZero(issuesAll.Length);
            Assert.AreEqual(issuesAll.Length, issuesNone.Length, "Code module is considering asset filter");
        }

        void TestFiltering(TestAsset testAsset, IssueCategory issueCategory)
        {
            if (!m_NullTestPassed)
            {
                Assert.Inconclusive();
            }

            var issuesAll = AnalyzeFiltered(_ => true, issueCategory);

            bool foundIssueForAsset = false;
            for (var i = 0; i < issuesAll.Length; i++)
            {
                if (issuesAll[i].RelativePath == testAsset.RelativePath)
                {
                    foundIssueForAsset = true;
                    break;
                }
            }
            if (!foundIssueForAsset)
            {
                Assert.Inconclusive("Unable to test asset filtering without reported issue to filter");
            }

            var issuesAllNull = AnalyzeFiltered(null, issueCategory);
            Assert.True(issuesAll.Length == issuesAllNull.Length, "Existence of filter predicate being used to filter");

            var issuesNone = AnalyzeFiltered(_ => false, issueCategory);
            Assert.Zero(issuesNone.Length, "Issues still created when all assets filtered out");

            var issuesTestAssetOnly = AnalyzeFiltered(str => str.Equals(testAsset.RelativePath), issueCategory);
            Assert.NotZero(issuesTestAssetOnly.Length);
            foreach (var issue in issuesTestAssetOnly)
            {
                if (issue.RelativePath != testAsset.RelativePath)
                {
                    Assert.Fail("Issue created for asset not included in filter");
                }
            }

            var issuesNotTestAsset = AnalyzeFiltered(str => !str.Equals(testAsset.RelativePath), issueCategory);
            Assert.NotZero(issuesNotTestAsset.Length);
            Assert.True(issuesNotTestAsset.Length + issuesTestAssetOnly.Length == issuesAll.Length, "Filtering for opposites generating inconsistent results");
            foreach (var issue in issuesNotTestAsset)
            {
                if (issue.RelativePath == testAsset.RelativePath)
                {
                    Assert.Fail("Issue created for asset excluded by filter");
                }
            }
        }

        [Test]
        public void AssetFiltering_AssetsModuleFiltering()
        {
            // this asset doesn't have any dependencies, so this works
            // if this asset had dependencies, this wouldn't work as the dependencies would get reported too
            //
            // given that the filtering is done in a generic way, testing just the asset diagnostic behavior should be good enough
            TestFiltering(m_SmallMeshAsset, IssueCategory.AssetIssue);
        }
    }
}
