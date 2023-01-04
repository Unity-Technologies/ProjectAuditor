using System.Linq;
using NUnit.Framework;

namespace Unity.ProjectAuditor.EditorTests
{
    class FindObjectsOfTypeTests : TestFixtureBase
    {
        TempAsset m_TempAsset;

        [OneTimeSetUp]
        public void SetUp()
        {
            m_TempAsset = new TempAsset("FindObjectsOfTypeClass.cs", @"
using UnityEngine;

class FindObjectsOfTypeClass
{
    void TestMethod()
    {
        // FindObjectsOfType
        UnityEngine.Object.FindObjectsOfType<Collider>();
        UnityEngine.GameObject.FindObjectsOfType<Collider>();

        UnityEngine.Object.FindObjectsOfType(typeof(Collider));
        UnityEngine.GameObject.FindObjectsOfType(typeof(Collider));

        // FindObjectOfType
        UnityEngine.Object.FindObjectOfType<Collider>();
        UnityEngine.GameObject.FindObjectOfType<Collider>();

        UnityEngine.Object.FindObjectOfType(typeof(Collider));
        UnityEngine.GameObject.FindObjectOfType(typeof(Collider));
    }
}
");
        }

        [Test]
        public void CodeAnalysis_FindObjectsOfType_IsReported()
        {
            var issues = AnalyzeAndFindAssetIssues(m_TempAsset);

            Assert.AreEqual(4, issues.Count(i => i.descriptor.id == "PAC0129"));
        }

        [Test]
        public void CodeAnalysis_FindObjectOfType_IsReported()
        {
            var issues = AnalyzeAndFindAssetIssues(m_TempAsset);

            Assert.AreEqual(4, issues.Count(i => i.descriptor.id == "PAC0234"));
        }
    }
}
