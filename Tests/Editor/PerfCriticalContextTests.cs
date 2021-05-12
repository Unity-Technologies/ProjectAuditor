using System;
using System.Linq;
using NUnit.Framework;

namespace UnityEditor.ProjectAuditor.EditorTests
{
    class PerfCriticalContextTests
    {
        TempAsset m_TempAssetIssueInClassInheritedFromMonoBehaviour;
        TempAsset m_TempAssetIssueInClassMethodCalledFromMonoBehaviourUpdate;
        TempAsset m_TempAssetIssueInMonoBehaviourUpdate;
        TempAsset m_TempAssetIssueInSimpleClass;
        TempAsset m_TempAssetShaderWarmupIssueIsCritical;

        [OneTimeSetUp]
        public void SetUp()
        {
            m_TempAssetIssueInSimpleClass = new TempAsset("IssueInSimpleClass.cs", @"
using UnityEngine;
class IssueInSimpleClass
{
    void Dummy()
    {
        Debug.Log(Camera.allCameras.Length);
    }
}
");

            m_TempAssetIssueInMonoBehaviourUpdate = new TempAsset("IssueInMonoBehaviourUpdate.cs", @"
using UnityEngine;
class IssueInMonoBehaviourUpdate : MonoBehaviour
{
    void Update()
    {
        Debug.Log(Camera.allCameras.Length);
    }
}
");

            m_TempAssetIssueInClassMethodCalledFromMonoBehaviourUpdate = new TempAsset(
                "IssueInClassMethodCalledFromMonoBehaviourUpdate.cs", @"
using UnityEngine;

class IssueInClassMethodCalledFromMonoBehaviourUpdate : MonoBehaviour
{
    class NestedClass
    {
        public void Dummy()
        {
            Debug.Log(Camera.allCameras.Length);
        }
    }

    NestedClass m_MyObj;
    void Update()
    {
        m_MyObj.Dummy();
    }
}
");

            m_TempAssetIssueInClassInheritedFromMonoBehaviour = new TempAsset(
                "IssueInClassInheritedFromMonoBehaviour.cs", @"
using UnityEngine;
class A : MonoBehaviour
{
}

class B : A
{
    void Update()
    {
        Debug.Log(Camera.allCameras.Length);
    }
}
");

            m_TempAssetShaderWarmupIssueIsCritical = new TempAsset("ShaderWarmUpIssueIsCritical.cs", @"
using UnityEngine;
class ShaderWarmUpIssueIsCritical
{
    void Start()
    {
        Shader.WarmupAllShaders();
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
        public void IssueInSimpleClass()
        {
            var issues = Utility.AnalyzeAndFindAssetIssues(m_TempAssetIssueInSimpleClass);
            var issue = issues.First();
            Assert.False(issue.isPerfCriticalContext);
        }

        [Test]
        public void IssueInMonoBehaviourUpdate()
        {
            var issues = Utility.AnalyzeAndFindAssetIssues(m_TempAssetIssueInMonoBehaviourUpdate);
            var issue = issues.First();
            Assert.True(issue.isPerfCriticalContext);
        }

        [Test]
        public void IssueInClassMethodCalledFromMonoBehaviourUpdate()
        {
            var issues =
                Utility.AnalyzeAndFindAssetIssues(
                    m_TempAssetIssueInClassMethodCalledFromMonoBehaviourUpdate);
            var issue = issues.First();
            Assert.True(issue.isPerfCriticalContext);
        }

        [Test]
        public void IssueInClassInheritedFromMonoBehaviour()
        {
            var issues =
                Utility.AnalyzeAndFindAssetIssues(
                    m_TempAssetIssueInClassInheritedFromMonoBehaviour);
            var issue = issues.First();
            Assert.True(issue.isPerfCriticalContext);
        }

        [Test]
        public void ShaderWarmupIssueIsCritical()
        {
            var issues =
                Utility.AnalyzeAndFindAssetIssues(
                    m_TempAssetShaderWarmupIssueIsCritical);
            var issue = issues.First();
            Assert.True(issue.isPerfCriticalContext);
        }
    }
}
