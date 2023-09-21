using System;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.CodeAnalysis;
using Unity.ProjectAuditor.Editor.InstructionAnalyzers;
using Unity.ProjectAuditor.Editor.Tests.Common;

namespace Unity.ProjectAuditor.EditorTests
{
    class AllocationTests : TestFixtureBase
    {
        TestAsset m_TestAssetObjectAllocation;
        TestAsset m_TestAssetClosureAllocation;
        TestAsset m_TestAssetArrayAllocation;
        TestAsset m_TestAssetMultidimensionalArrayAllocation;
        TestAsset m_TestAssetParamsArrayAllocation;

        [OneTimeSetUp]
        public void SetUp()
        {
            m_TestAssetObjectAllocation = new TestAsset("ObjectAllocation.cs", @"
class ObjectAllocation
{
    static ObjectAllocation Dummy()
    {
        // explicit object allocation
        return new ObjectAllocation();
    }
}
");

            m_TestAssetClosureAllocation = new TestAsset("ClosureAllocation.cs", @"
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


            m_TestAssetArrayAllocation = new TestAsset("ArrayAllocation.cs", @"
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

            m_TestAssetMultidimensionalArrayAllocation = new TestAsset("MultidimensionalArrayAllocation.cs", @"
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

            m_TestAssetParamsArrayAllocation = new TestAsset("ParamsArrayAllocation.cs", @"
class ParamsArrayAllocation
{
    void MethodWithParams(params object[] args)
    {
    }

    void Dummy(object C)
    {
        // implicit array allocation
        MethodWithParams(null, null);
    }
}
");
        }

        [Test]
        public void CodeAnalysis_NewObject_IsReported()
        {
            var issues = AnalyzeAndFindAssetIssues(m_TestAssetObjectAllocation);

            Assert.AreEqual(1, issues.Count());

            var allocationIssue = issues.First();

            Assert.AreEqual("'ObjectAllocation' allocation", allocationIssue.description);
            Assert.AreEqual(IssueCategory.Code, allocationIssue.category);
        }

        [Test]
        public void CodeAnalysis_ClosureAllocation_IsReported()
        {
            var issues = AnalyzeAndFindAssetIssues(m_TestAssetClosureAllocation);

            Assert.AreEqual(1, issues.Count());

            var issue = issues.First();

            Assert.AreNotEqual(MonoCecilHelper.HiddenLine, issue.line);
            Assert.AreEqual("Closure allocation in 'ClosureAllocation.Dummy'", issue.description);
            Assert.AreEqual(IssueCategory.Code, issue.category);
        }

        [Test]
        public void CodeAnalysis_NewArray_IsReported()
        {
            var issues = AnalyzeAndFindAssetIssues(m_TestAssetArrayAllocation);
            Assert.AreEqual(1, issues.Count());

            var allocationIssue = issues.First();

            Assert.AreEqual("'Int32' array allocation", allocationIssue.description);
            Assert.AreEqual(IssueCategory.Code, allocationIssue.category);
        }

        [Test]
        public void CodeAnalysis_NewMultidimensionalArray_IsReported()
        {
            var issues = AnalyzeAndFindAssetIssues(m_TestAssetMultidimensionalArrayAllocation);
            Assert.AreEqual(1, issues.Count());

            var allocationIssue = issues.First();

            Assert.AreEqual("'System.Int32[0...,0...]' allocation", allocationIssue.description);
            Assert.AreEqual(IssueCategory.Code, allocationIssue.category);
        }

        [Test]
        public void CodeAnalysis_NewParamsArray_IsReported()
        {
            var issues = AnalyzeAndFindAssetIssues(m_TestAssetParamsArrayAllocation);
            Assert.AreEqual(2, issues.Count());

            Assert.AreEqual(IssueCategory.Code, issues[0].category);
            Assert.AreEqual(AllocationAnalyzer.PAC2004, issues[0].Id);
            Assert.AreEqual("'Object' array allocation", issues[0].description);

            Assert.AreEqual(IssueCategory.Code, issues[1].category);
            Assert.AreEqual(AllocationAnalyzer.PAC2005, issues[1].Id);
            Assert.AreEqual("Parameters array 'Object[] args' allocation", issues[1].description);
        }
    }
}
