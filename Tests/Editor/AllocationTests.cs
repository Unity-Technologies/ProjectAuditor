using System;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.CodeAnalysis;

namespace Unity.ProjectAuditor.EditorTests
{
    class AllocationTests
    {
        TempAsset m_TempAssetObjectAllocation;
        TempAsset m_TempAssetClosureAllocation;
        TempAsset m_TempAssetArrayAllocation;
        TempAsset m_TempAssetMultidimensionalArrayAllocation;
        TempAsset m_TempAssetParamsArrayAllocation;

        [OneTimeSetUp]
        public void SetUp()
        {
            m_TempAssetObjectAllocation = new TempAsset("ObjectAllocation.cs", @"
class ObjectAllocation
{
    static ObjectAllocation Dummy()
    {
        // explicit object allocation
        return new ObjectAllocation();
    }
}
");

            m_TempAssetClosureAllocation = new TempAsset("ClosureAllocation.cs", @"
using UnityEngine;
using System;
class ClosureAllocation
{
    void Dummy()
    {
        int x = 1;
        Func<int, int> f = y => y * x;
    }
}
");


            m_TempAssetArrayAllocation = new TempAsset("ArrayAllocation.cs", @"
class ArrayAllocation
{
    int[] array;
    void Dummy()
    {
        // explicit array allocation
        array = new int[1];
    }
}
");

            m_TempAssetMultidimensionalArrayAllocation = new TempAsset("MultidimensionalArrayAllocation.cs", @"
class MultidimensionalArrayAllocation
{
    int[,] array;
    void Dummy()
    {
        // explicit array allocation
        array = new int[1,1];
    }
}
");

            m_TempAssetParamsArrayAllocation = new TempAsset("ParamsArrayAllocation.cs", @"
class ParamsArrayAllocation
{
    void DummyImpl(params object[] args)
    {
    }

    void Dummy(object C)
    {
        // implicit array allocation
        DummyImpl(null, null);
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
        public void CodeAnalysis_NewObject_IsReported()
        {
            var issues = Utility.AnalyzeAndFindAssetIssues(m_TempAssetObjectAllocation);

            Assert.AreEqual(1, issues.Count());

            var allocationIssue = issues.First();

            Assert.AreEqual("'ObjectAllocation' allocation", allocationIssue.description);
            Assert.AreEqual(IssueCategory.Code, allocationIssue.category);
        }

        [Test]
        public void CodeAnalysis_ClosureAllocation_IsReported()
        {
            var issues = Utility.AnalyzeAndFindAssetIssues(m_TempAssetClosureAllocation);

            Assert.AreEqual(1, issues.Count());

            var issue = issues.First();

            Assert.AreNotEqual(MonoCecilHelper.HiddenLine, issue.line);
            Assert.AreEqual("Closure allocation in 'ClosureAllocation.Dummy'", issue.description);
            Assert.AreEqual(IssueCategory.Code, issue.category);
        }

        [Test]
        public void CodeAnalysis_NewArray_IsReported()
        {
            var issues = Utility.AnalyzeAndFindAssetIssues(m_TempAssetArrayAllocation);
            Assert.AreEqual(1, issues.Count());

            var allocationIssue = issues.First();

            Assert.AreEqual("'Int32' array allocation", allocationIssue.description);
            Assert.AreEqual(IssueCategory.Code, allocationIssue.category);
        }

        [Test]
        public void CodeAnalysis_NewMultidimensionalArray_IsReported()
        {
            var issues = Utility.AnalyzeAndFindAssetIssues(m_TempAssetMultidimensionalArrayAllocation);
            Assert.AreEqual(1, issues.Count());

            var allocationIssue = issues.First();

            Assert.AreEqual("'System.Int32[0...,0...]' allocation", allocationIssue.description);
            Assert.AreEqual(IssueCategory.Code, allocationIssue.category);
        }

        [Test]
        public void CodeAnalysis_NewParamsArray_IsReported()
        {
            var issues = Utility.AnalyzeAndFindAssetIssues(m_TempAssetParamsArrayAllocation);
            Assert.AreEqual(1, issues.Count());

            var allocationIssue = issues.First();

            Assert.AreEqual("'Object' array allocation", allocationIssue.description);
            Assert.AreEqual(IssueCategory.Code, allocationIssue.category);
        }
    }
}
