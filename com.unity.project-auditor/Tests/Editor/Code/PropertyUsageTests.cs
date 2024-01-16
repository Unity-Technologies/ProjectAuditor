using System;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.AssemblyUtils;
using Unity.ProjectAuditor.Editor.Modules;
using Unity.ProjectAuditor.Editor.CodeAnalysis;
using Unity.ProjectAuditor.Editor.Tests.Common;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEngine;

namespace Unity.ProjectAuditor.EditorTests
{
    class PropertyUsageTests : TestFixtureBase
    {
        TestAsset m_TestAssetObjectName;
        TestAsset m_TestAssetBaseTypePropertyUsage;
        TestAsset m_TestAssetUxmlAttributeDescriptionPropertyUsage;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            m_TestAssetObjectName = new TestAsset("ObjectNameTest.cs", @"
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

            m_TestAssetBaseTypePropertyUsage = new TestAsset("BaseTypePropertyUsage.cs", @"
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

            m_TestAssetUxmlAttributeDescriptionPropertyUsage = new TestAsset("UxmlAttributeDescriptionPropertyUsage.cs", @"
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
            AnalyzeTestAssets();
        }

        [Test]
        public void CodeAnalysis_PropertyName_IsReported()
        {
            var issues = GetIssuesForAsset(m_TestAssetObjectName);

            var propertyNameIssues = issues.Where(i => i.Id == "PAC0231").ToArray();

            Assert.AreEqual(3, propertyNameIssues.Length);
            Assert.True(propertyNameIssues.All(i => i.Description.Equals("'UnityEngine.Object.name' usage")));
        }

        [Test]
        public void CodeAnalysis_PropertyOfBaseType_IsReported()
        {
            var issues = GetIssuesForAsset(m_TestAssetBaseTypePropertyUsage);

            var propertyOfBaseTypeIssues = issues.Where(
                i => i.Id == "PAC0039" || i.Id == "PAC0084" || i.Id == "PAC0085")
                .ToArray();

            Assert.AreEqual(6, propertyOfBaseTypeIssues.Length);
        }

        [Test]
        public void CodeAnalysis_PropertyUxmlAttributeDescription_IsReported()
        {
            var issues = GetIssuesForAsset(m_TestAssetUxmlAttributeDescriptionPropertyUsage);

            var propertyUxmlAttributeIssues = issues.Where(i => i.Id == "PAC0191").ToArray();

            Assert.AreEqual(1, propertyUxmlAttributeIssues.Length);
            Assert.AreEqual("'UnityEngine.UIElements.UxmlAttributeDescription.obsoleteNames' usage", propertyUxmlAttributeIssues[0].Description);
        }
    }
}
