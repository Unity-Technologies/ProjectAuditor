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
        public void OneTimeSetUp()
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
            AnalyzeTestAssets();
        }

        [Test]
        public void CallTree_Root_IsValid()
        {
            var issues = GetIssuesForAsset(m_TestAsset).ToArray();

            Assert.AreEqual(1, issues.Length);

            var root = issues[0].Dependencies as CallTreeNode;

            Assert.NotNull(root);
            Assert.AreEqual("System.Object MyTestNamespace.SimpleTest::CallerMethod()", root.MethodFullName);
            Assert.AreEqual(AssemblyInfo.DefaultAssemblyFileName, root.AssemblyName);
            Assert.AreEqual("MyTestNamespace.SimpleTest", root.TypeFullName);
            Assert.AreEqual("CallerMethod", root.PrettyMethodName);
            Assert.AreEqual("SimpleTest", root.PrettyTypeName);
            Assert.AreEqual(0, root.NumChildren);
        }

        [Test]
        public void CallTree_Hierarchy_IsNotRecursive()
        {
            var issues = GetIssuesForAsset(m_TestAssetRecursive).ToArray();

            var root = issues[0].Dependencies as CallTreeNode;

            Assert.NotNull(root);
            Assert.AreEqual("X", root.PrettyMethodName);
            Assert.AreEqual("RecursiveTest", root.PrettyTypeName);
            Assert.AreEqual(0, root.GetChild().NumChildren);
        }

        [Test]
        public void CallTree_SameSubHierarchy_IsUnique()
        {
            var issues = GetIssuesForAsset(m_TestAssetHierarchy).ToArray();

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
