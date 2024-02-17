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
        public void OneTimeSetUp()
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
            AnalyzeTestAssets();
        }

        [Test]
        public void CodeAnalysis_NewObject_IsReported()
        {
            var issues = GetIssuesForAsset(m_TestAssetObjectAllocation);

            Assert.AreEqual(1, issues.Count());

            var allocationIssue = issues.First();

            Assert.AreEqual("'ObjectAllocation' allocation", allocationIssue.Description);
            Assert.AreEqual(IssueCategory.Code, allocationIssue.Category);
        }

        [Test]
        public void CodeAnalysis_ClosureAllocation_IsReported()
        {
            var issues = GetIssuesForAsset(m_TestAssetClosureAllocation);

            Assert.AreEqual(1, issues.Count());

            var issue = issues.First();

            Assert.AreNotEqual(MonoCecilHelper.HiddenLine, issue.Line);
            Assert.AreEqual("Closure allocation in 'ClosureAllocation.Dummy'", issue.Description);
            Assert.AreEqual(IssueCategory.Code, issue.Category);
        }

        [Test]
        public void CodeAnalysis_NewArray_IsReported()
        {
            var issues = GetIssuesForAsset(m_TestAssetArrayAllocation);
            Assert.AreEqual(1, issues.Count());

            var allocationIssue = issues.First();

            Assert.AreEqual("'Int32' array allocation", allocationIssue.Description);
            Assert.AreEqual(IssueCategory.Code, allocationIssue.Category);
        }

        [Test]
        public void CodeAnalysis_NewMultidimensionalArray_IsReported()
        {
            var issues = GetIssuesForAsset(m_TestAssetMultidimensionalArrayAllocation);
            Assert.AreEqual(1, issues.Count());

            var allocationIssue = issues.First();

            Assert.AreEqual("'System.Int32[0...,0...]' allocation", allocationIssue.Description);
            Assert.AreEqual(IssueCategory.Code, allocationIssue.Category);
        }

        [Test]
        public void CodeAnalysis_NewParamsArray_IsReported()
        {
            var issues = GetIssuesForAsset(m_TestAssetParamsArrayAllocation);
            Assert.AreEqual(2, issues.Count());

            Assert.AreEqual(IssueCategory.Code, issues[0].Category);
            Assert.AreEqual(AllocationAnalyzer.PAC2004, issues[0].Id.ToString());
            Assert.AreEqual("'Object' array allocation", issues[0].Description);

            Assert.AreEqual(IssueCategory.Code, issues[1].Category);
            Assert.AreEqual(AllocationAnalyzer.PAC2005, issues[1].Id.ToString());
            Assert.AreEqual("Parameters array 'Object[] args' allocation", issues[1].Description);
        }
    }
}
