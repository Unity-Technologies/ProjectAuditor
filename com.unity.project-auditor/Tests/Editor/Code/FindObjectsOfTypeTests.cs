using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor.Tests.Common;

namespace Unity.ProjectAuditor.EditorTests
{
    class FindObjectsOfTypeTests : TestFixtureBase
    {
        TestAsset m_TestAsset;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            m_TestAsset = new TestAsset("FindObjectsOfTypeClass.cs", @"
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
            AnalyzeTestAssets();
        }

        [Test]
        public void CodeAnalysis_FindObjectsOfType_IsReported()
        {
            var issues = GetIssuesForAsset(m_TestAsset).Where(i => i.Id == "PAC0129").ToArray();

            Assert.AreEqual(4, issues.Length);
            Assert.AreEqual(issues[0].Description, "'UnityEngine.Object.FindObjectsOfType<UnityEngine.Collider>' usage");
            Assert.AreEqual(issues[1].Description, "'UnityEngine.Object.FindObjectsOfType<UnityEngine.Collider>' usage");
            Assert.AreEqual(issues[2].Description, "'UnityEngine.Object.FindObjectsOfType' usage");
            Assert.AreEqual(issues[3].Description, "'UnityEngine.Object.FindObjectsOfType' usage");
        }

        [Test]
        public void CodeAnalysis_FindObjectOfType_IsReported()
        {
            var issues = GetIssuesForAsset(m_TestAsset).Where(i => i.Id == "PAC0234").ToArray();

            Assert.AreEqual(4, issues.Length);
            Assert.AreEqual(issues[0].Description, "'UnityEngine.Object.FindObjectOfType<UnityEngine.Collider>' usage");
            Assert.AreEqual(issues[1].Description, "'UnityEngine.Object.FindObjectOfType<UnityEngine.Collider>' usage");
            Assert.AreEqual(issues[2].Description, "'UnityEngine.Object.FindObjectOfType' usage");
            Assert.AreEqual(issues[3].Description, "'UnityEngine.Object.FindObjectOfType' usage");
        }
    }
}
