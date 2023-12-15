using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor.Tests.Common;

namespace Unity.ProjectAuditor.EditorTests
{
    class FindObjectsOfTypeTests : TestFixtureBase
    {
        TestAsset m_TestAsset;

        [OneTimeSetUp]
        public void SetUp()
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
            AnalyzeTempAssetsFolder();
        }

        [Test]
        public void CodeAnalysis_FindObjectsOfType_IsReported()
        {
            Assert.AreEqual(4, m_CodeDiagnostics.Count(i => i.Id == "PAC0129"));
        }

        [Test]
        public void CodeAnalysis_FindObjectOfType_IsReported()
        {
            Assert.AreEqual(4, m_CodeDiagnostics.Count(i => i.Id == "PAC0234"));
        }
    }
}
