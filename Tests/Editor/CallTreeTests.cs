using NUnit.Framework;
using Unity.ProjectAuditor.Editor.CodeAnalysis;

namespace Unity.ProjectAuditor.EditorTests
{
    class CallTreeTests
    {
        TempAsset m_TempAsset;
        TempAsset m_TempAssetHierarchy;
        TempAsset m_TempAssetRecursive;

        [OneTimeSetUp]
        public void SetUp()
        {
            m_TempAsset = new TempAsset("RootTest.cs", @"
using System;
class RootTest
{
    Object CallerMethod()
    {
        return 5;
    }
}");

            m_TempAssetRecursive = new TempAsset("RecursiveTest.cs", @"
using System;
class RecursiveTest
{
    Object X()
    {
        X();
        return 5;
    }
}");

// the same sub-hierarchy should only be computed once
// Issue:     X      Y
//            |      |
//            A      B
//            |      |
//            B      C
//            |
//            C
//
//  method C calling into B should be the same for both issue X and Y

            m_TempAssetHierarchy = new TempAsset("HierarchyTest.cs", @"
using System;
class HierarchyTest
{
    Object X()
    {
        return 5;
    }

    Object Y()
    {
        return 5;
    }

    void A() { X(); }
    void B() { A(); Y(); }
    void C() { B(); }
}");
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            TempAsset.Cleanup();
        }

        [Test]
        public void CallTree_Root_IsValid()
        {
            var issues = Utility.AnalyzeAndFindAssetIssues(m_TempAsset);

            Assert.AreEqual(1, issues.Length);

            var root = issues[0].dependencies as CallTreeNode;

            Assert.NotNull(root);
            Assert.AreEqual("System.Object RootTest::CallerMethod()", root.name);
            Assert.AreEqual("Assembly-CSharp.dll", root.assemblyName);
            Assert.AreEqual("CallerMethod", root.methodName);
            Assert.AreEqual("RootTest", root.typeName);
            Assert.AreEqual(1, root.GetNumChildren());
            Assert.AreEqual(0, root.GetChild().GetNumChildren());
        }

        [Test]
        public void CallTree_Hierarchy_IsNotRecursive()
        {
            var issues = Utility.AnalyzeAndFindAssetIssues(m_TempAssetRecursive);

            var root = issues[0].dependencies as CallTreeNode;

            Assert.NotNull(root);
            Assert.AreEqual("X", root.methodName);
            Assert.AreEqual("RecursiveTest", root.typeName);
            Assert.AreEqual(0, root.GetChild().GetChild().GetNumChildren());
        }

        [Test]
        public void CallTree_SubHierarchy_IsSame()
        {
            var issues = Utility.AnalyzeAndFindAssetIssues(m_TempAssetHierarchy);

            Assert.AreEqual(2, issues.Length);

            var rootX = issues[0].dependencies as CallTreeNode;
            var rootY = issues[1].dependencies as CallTreeNode;

            Assert.NotNull(rootX);
            Assert.NotNull(rootY);

            var A2X = rootX.GetChild().GetChild();
            var B2Y = rootY.GetChild().GetChild();

            // check C=>B nodes are the same
            Assert.True(ReferenceEquals(A2X.GetChild().GetChild(), B2Y.GetChild()));
        }
    }
}
