using System;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.TestUtils;

namespace Unity.ProjectAuditor.EditorTests
{
    class InstantiateAddComponentTests : TestFixtureBase
    {
        TestAsset m_TestAssetAddComponent;
        TestAsset m_TestAssetAddComponentGeneric;
        TestAsset m_TestAssetInstantiate;

        [OneTimeSetUp]
        public void SetUp()
        {
            m_TestAssetInstantiate = new TestAsset("InstantiateObject.cs", @"
using UnityEngine;
class InstantiateObject
{
    public GameObject Prefab;
    void Test()
    {
        Object.Instantiate(Prefab);
    }
}
");

            m_TestAssetAddComponent = new TestAsset("AddComponentToGameObject.cs", @"
using UnityEngine;
class AddComponentToGameObject
{
    public GameObject Instance;
    void Test()
    {
        Instance.AddComponent(typeof(Rigidbody));
    }
}
");

            m_TestAssetAddComponentGeneric = new TestAsset("AddComponentGeneric.cs", @"
using UnityEngine;
class AddComponentGeneric : MonoBehaviour
{
    public GameObject Instance;
    void Test()
    {
        Instance.AddComponent<Rigidbody>();
    }
}
");
        }

        [Test]
        public void CodeAnalysis_Instantiate_IsReported()
        {
            var issues = AnalyzeAndFindAssetIssues(m_TestAssetInstantiate);

            Assert.AreEqual(1, issues.Length);
            Assert.AreEqual("System.Void InstantiateObject::Test()", issues[0].GetContext());
            Assert.AreEqual("'UnityEngine.Object.Instantiate' usage (with generic argument 'UnityEngine.GameObject')", issues[0].description);
        }

        [Test]
        public void CodeAnalysis_AddComponent_IsReported()
        {
            var issues = AnalyzeAndFindAssetIssues(m_TestAssetAddComponent);

            Assert.AreEqual(1, issues.Length);
            Assert.AreEqual("System.Void AddComponentToGameObject::Test()", issues[0].GetContext());
            Assert.AreEqual("'UnityEngine.GameObject.AddComponent' usage", issues[0].description);
        }

        [Test]
        public void CodeAnalysis_AddComponentGeneric_IsReported()
        {
            var issues = AnalyzeAndFindAssetIssues(m_TestAssetAddComponentGeneric);

            Assert.AreEqual(1, issues.Length);
            Assert.AreEqual("System.Void AddComponentGeneric::Test()", issues[0].GetContext());
            Assert.AreEqual("'UnityEngine.GameObject.AddComponent' usage (with generic argument 'UnityEngine.Rigidbody')", issues[0].description);
        }
    }
}
