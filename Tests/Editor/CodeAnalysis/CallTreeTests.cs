using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor.CodeAnalysis;
using Unity.ProjectAuditor.Editor.AssemblyUtils;
using Unity.ProjectAuditor.Editor.Tests.Common;

namespace Unity.ProjectAuditor.EditorTests
{
    class CallTreeTests : TestFixtureBase
    {
        TestAsset m_TestAsset;
        TestAsset m_TestAssetHierarchy;
        TestAsset m_TestAssetRecursive;

        [OneTimeSetUp]
        public void SetUp()
        {
            m_TestAsset = new TestAsset("SimpleTest.cs", @"
using System;

namespace MyTestNamespace
{
    class SimpleTest
    {
        Object CallerMethod()
        {
            return 5;
        }
    }
}");

            m_TestAssetRecursive = new TestAsset("RecursiveTest.cs", @"
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

            m_TestAssetHierarchy = new TestAsset("HierarchyTest.cs", @"
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
            AnalyzeTempAssetsFolder();
        }

        [Test]
        public void CallTree_Root_IsValid()
        {
            var issues = FindTestAssetIssues(m_TestAsset).ToArray();

            Assert.AreEqual(1, issues.Length);

            var root = issues[0].Dependencies as CallTreeNode;

            Assert.NotNull(root);
            Assert.AreEqual("System.Object MyTestNamespace.SimpleTest::CallerMethod()", root.methodFullName);
            Assert.AreEqual(AssemblyInfo.DefaultAssemblyFileName, root.assemblyName);
            Assert.AreEqual("MyTestNamespace.SimpleTest", root.typeFullName);
            Assert.AreEqual("CallerMethod", root.prettyMethodName);
            Assert.AreEqual("SimpleTest", root.prettyTypeName);
            Assert.AreEqual(0, root.GetNumChildren());
        }

        [Test]
        public void CallTree_Hierarchy_IsNotRecursive()
        {
            var issues = FindTestAssetIssues(m_TestAssetRecursive).ToArray();

            var root = issues[0].Dependencies as CallTreeNode;

            Assert.NotNull(root);
            Assert.AreEqual("X", root.prettyMethodName);
            Assert.AreEqual("RecursiveTest", root.prettyTypeName);
            Assert.AreEqual(0, root.GetChild().GetNumChildren());
        }

        [Test]
        public void CallTree_SameSubHierarchy_IsUnique()
        {
            var issues = FindTestAssetIssues(m_TestAssetHierarchy).ToArray();

            Assert.AreEqual(2, issues.Length);

            var rootX = issues[0].Dependencies as CallTreeNode;
            var rootY = issues[1].Dependencies as CallTreeNode;

            Assert.NotNull(rootX);
            Assert.NotNull(rootY);

            var A2X = rootX.GetChild();
            var B2Y = rootY.GetChild();

            // check C=>B nodes are the same
            Assert.True(ReferenceEquals(A2X.GetChild().GetChild(), B2Y.GetChild()));
        }
    }
}
