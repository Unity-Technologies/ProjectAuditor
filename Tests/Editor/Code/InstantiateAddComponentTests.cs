using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Tests.Common;

namespace Unity.ProjectAuditor.EditorTests
{
    class InstantiateAddComponentTests : TestFixtureBase
    {
        TestAsset m_TestAssetAddComponent;
        TestAsset m_TestAssetAddComponentGeneric;
        TestAsset m_TestAssetInstantiate;

        [OneTimeSetUp]
        public void OneTimeSetUp()
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
            AnalyzeTestAssets();
        }

        [Test]
        public void CodeAnalysis_Instantiate_IsReported()
        {
            var issues = GetIssuesForAsset(m_TestAssetInstantiate);

            Assert.AreEqual(1, issues.Length);
            Assert.AreEqual("System.Void InstantiateObject::Test()", issues[0].GetContext());
            Assert.AreEqual("'UnityEngine.Object.Instantiate<UnityEngine.GameObject>' usage", issues[0].Description);
        }

        [Test]
        public void CodeAnalysis_AddComponent_IsReported()
        {
            var issues = GetIssuesForAsset(m_TestAssetAddComponent);

            Assert.AreEqual(1, issues.Length);
            Assert.AreEqual("System.Void AddComponentToGameObject::Test()", issues[0].GetContext());
            Assert.AreEqual("'UnityEngine.GameObject.AddComponent' usage", issues[0].Description);
        }

        [Test]
        public void CodeAnalysis_AddComponentGeneric_IsReported()
        {
            var issues = GetIssuesForAsset(m_TestAssetAddComponentGeneric);

            Assert.AreEqual(1, issues.Length);
            Assert.AreEqual("System.Void AddComponentGeneric::Test()", issues[0].GetContext());
            Assert.AreEqual("'UnityEngine.GameObject.AddComponent<UnityEngine.Rigidbody>' usage", issues[0].Description);
        }
    }
}
