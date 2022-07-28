using System;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.AssemblyUtils;
using Unity.ProjectAuditor.Editor.Modules;
using Unity.ProjectAuditor.Editor.CodeAnalysis;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEngine;

namespace Unity.ProjectAuditor.EditorTests
{
    class PropertyUsageTests : TestFixtureBase
    {
        TempAsset m_TempAssetObjectName;
        TempAsset m_TempAssetBaseTypePropertyUsage;
#if UNITY_2019_1_OR_NEWER
        TempAsset m_TempAssetUxmlAttributeDescriptionPropertyUsage;
#endif

        [OneTimeSetUp]
        public void SetUp()
        {
            m_TempAssetObjectName = new TempAsset("ObjectNameTest.cs", @"
using UnityEngine;
class ObjectNameTest : MonoBehaviour
{
    void Start()
    {
        Debug.Log(gameObject.name);
        Debug.Log(transform.name);
        Debug.Log(this.name);
    }
}
");

            m_TempAssetBaseTypePropertyUsage = new TempAsset("BaseTypePropertyUsage.cs", @"
using UnityEngine;
class BaseTypePropertyUsage
{
    void Test()
    {
        Material material = null;
        Material[] materials;
        Material[] sharedMaterials;

        // check base class Renderer
        Renderer baseClassRenderer = null;
        material = baseClassRenderer.material;
        materials = baseClassRenderer.materials;
        sharedMaterials = baseClassRenderer.sharedMaterials;

        // check derived class LineRenderer
        LineRenderer subClassRenderer = null;
        material = subClassRenderer.material;
        materials = subClassRenderer.materials;
        sharedMaterials = subClassRenderer.sharedMaterials;
    }
}
");

#if UNITY_2019_1_OR_NEWER
            m_TempAssetUxmlAttributeDescriptionPropertyUsage = new TempAsset("UxmlAttributeDescriptionPropertyUsage.cs", @"
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

class UxmlAttributeDescriptionPropertyUsage
{
    UxmlFloatAttributeDescription desc;
    IEnumerable<string> names;

    void Test()
    {
        names = desc.obsoleteNames;
    }
}
");
#endif
        }

        [Test]
        public void CodeAnalysis_PropertyName_IsReported()
        {
            var issues = AnalyzeAndFindAssetIssues(m_TempAssetObjectName);

            Assert.AreEqual(3, issues.Length);
            Assert.True(issues.All(i => i.description.Equals("'UnityEngine.Object.name' usage")));
        }

        [Test]
        public void CodeAnalysis_PropertyOfBaseType_IsReported()
        {
            var issues = AnalyzeAndFindAssetIssues(m_TempAssetBaseTypePropertyUsage);

            Assert.AreEqual(6, issues.Length);
        }

#if UNITY_2019_1_OR_NEWER
        [Test]
        public void CodeAnalysis_PropertyUxmlAttributeDescription_IsReported()
        {
            var issues = AnalyzeAndFindAssetIssues(m_TempAssetUxmlAttributeDescriptionPropertyUsage);

            Assert.AreEqual(1, issues.Length);
            Assert.AreEqual("'UnityEngine.UIElements.UxmlAttributeDescription.obsoleteNames' usage", issues[0].description);
        }

#endif
    }
}
