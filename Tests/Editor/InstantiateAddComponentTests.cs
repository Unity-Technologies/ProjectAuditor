using System;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;

namespace Unity.ProjectAuditor.EditorTests
{
    class InstantiateAddComponentTests : TestFixtureBase
    {
        TempAsset m_TempAssetAddComponent;
        TempAsset m_TempAssetAddComponentGeneric;
        TempAsset m_TempAssetInstantiate;

        [OneTimeSetUp]
        public void SetUp()
        {
            m_TempAssetInstantiate = new TempAsset("InstantiateObject.cs", @"
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

            m_TempAssetAddComponent = new TempAsset("AddComponentToGameObject.cs", @"
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

            m_TempAssetAddComponentGeneric = new TempAsset("AddComponentGeneric.cs", @"
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
            var issues = AnalyzeAndFindAssetIssues(m_TempAssetInstantiate);

            Assert.AreEqual(1, issues.Length);
            Assert.AreEqual("System.Void InstantiateObject::Test()", issues[0].GetContext());
            Assert.AreEqual("'UnityEngine.Object.Instantiate' usage (with generic argument 'UnityEngine.GameObject')", issues[0].description);
        }

        [Test]
        public void CodeAnalysis_AddComponent_IsReported()
        {
            var issues = AnalyzeAndFindAssetIssues(m_TempAssetAddComponent);

            Assert.AreEqual(1, issues.Length);
            Assert.AreEqual("System.Void AddComponentToGameObject::Test()", issues[0].GetContext());
            Assert.AreEqual("'UnityEngine.GameObject.AddComponent' usage", issues[0].description);
        }

        [Test]
        public void CodeAnalysis_AddComponentGeneric_IsReported()
        {
            var issues = AnalyzeAndFindAssetIssues(m_TempAssetAddComponentGeneric);

            Assert.AreEqual(1, issues.Length);
            Assert.AreEqual("System.Void AddComponentGeneric::Test()", issues[0].GetContext());
            Assert.AreEqual("'UnityEngine.GameObject.AddComponent' usage (with generic argument 'UnityEngine.Rigidbody')", issues[0].description);
        }
    }
}
