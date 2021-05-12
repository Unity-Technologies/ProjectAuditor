using System;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor.CodeAnalysis;

namespace UnityEditor.ProjectAuditor.EditorTests
{
    class InstantiateAddComponentTests
    {
        TempAsset m_TempAssetAddComponent;
        TempAsset m_TempAssetInstantiate;

        [OneTimeSetUp]
        public void SetUp()
        {
            m_TempAssetInstantiate = new TempAsset("InstantiateObject.cs", @"
using UnityEngine;
class InstantiateObject : MonoBehaviour
{
    public GameObject Prefab;
    public GameObject Instance;
    void Start()
    {
        Instance = Instantiate(Prefab);
    }
}
");

            m_TempAssetAddComponent = new TempAsset("AddComponentToGameObject.cs", @"
using UnityEngine;
class AddComponentToGameObject : MonoBehaviour
{
    public GameObject Instance;
    void Start()
    {
        Instance.AddComponent<Rigidbody>();
    }
}
");
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            TempAsset.Cleanup();
        }

        [Test]
        public void InstantiateIssueIsFound()
        {
            var issues = Utility.AnalyzeAndFindAssetIssues(m_TempAssetInstantiate);

            Assert.AreEqual(1, issues.Count());

            Assert.True(issues.First().GetCallingMethod().Equals("System.Void InstantiateObject::Start()"));
        }

        [Test]
        public void AddComponentIssueIsFound()
        {
            var issues = Utility.AnalyzeAndFindAssetIssues(m_TempAssetAddComponent);

            Assert.AreEqual(1, issues.Count());

            Assert.True(issues.First().GetCallingMethod().Equals("System.Void AddComponentToGameObject::Start()"));
        }
    }
}
