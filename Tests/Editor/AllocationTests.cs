using System;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;

namespace UnityEditor.ProjectAuditor.EditorTests
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
        public void ObjectAllocationIsFound()
        {
            var issues = Utility.AnalyzeAndFindAssetIssues(m_TempAssetObjectAllocation);

            Assert.AreEqual(1, issues.Count());

            var allocationIssue = issues.First();

            Assert.True(allocationIssue.description.Equals("'ObjectAllocation' allocation"));
            Assert.AreEqual(IssueCategory.Code, allocationIssue.category);
        }

        [Test]
        public void ClosureAllocationIsFound()
        {
            var issues = Utility.AnalyzeAndFindAssetIssues(m_TempAssetClosureAllocation);

            Assert.AreEqual(1, issues.Count());

            var issue = issues.First();

            Assert.AreNotEqual(16707566, issue.line);
            Assert.True(issue.description.Equals("'ClosureAllocation' closure allocation"));
            Assert.True(issue.name.Equals("ClosureAllocation.Dummy"));
            Assert.AreEqual(IssueCategory.Code, issue.category);
        }

        [Test]
        public void ArrayAllocationIsFound()
        {
            var issues = Utility.AnalyzeAndFindAssetIssues(m_TempAssetArrayAllocation);
            Assert.AreEqual(1, issues.Count());

            var allocationIssue = issues.First();

            Assert.True(allocationIssue.description.Equals("'Int32' array allocation"));
            Assert.AreEqual(IssueCategory.Code, allocationIssue.category);
        }

        [Test]
        public void MultidimensionalArrayAllocationIsFound()
        {
            var issues = Utility.AnalyzeAndFindAssetIssues(m_TempAssetMultidimensionalArrayAllocation);
            Assert.AreEqual(1, issues.Count());

            var allocationIssue = issues.First();

            Assert.True(allocationIssue.description.Equals("'System.Int32[0...,0...]' allocation"));
            Assert.AreEqual(IssueCategory.Code, allocationIssue.category);
        }

        [Test]
        public void ParamsArrayAllocationIsFound()
        {
            var issues = Utility.AnalyzeAndFindAssetIssues(m_TempAssetParamsArrayAllocation);
            Assert.AreEqual(1, issues.Count());

            var allocationIssue = issues.First();

            Assert.True(allocationIssue.description.Equals("'Object' array allocation"));
            Assert.AreEqual(IssueCategory.Code, allocationIssue.category);
        }
    }
}
