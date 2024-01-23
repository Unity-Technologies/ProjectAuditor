using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Tests.Common;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.ProjectAuditor.EditorTests
{
    [Serializable]
    class PerfCriticalContextTests : TestFixtureBase
    {
        TestAsset m_TestAssetIssueInClassInheritedFromMonoBehaviour;
        TestAsset m_TestAssetIssueInClassMethodCalledFromMonoBehaviourUpdate;
        TestAsset m_TestAssetIssueInMonoBehaviourUpdate;
        TestAsset m_TestAssetIssueInMonoBehaviourOnAnimatorMove;
        TestAsset m_TestAssetIssueInMonoBehaviourOnRenderObject;
        TestAsset m_TestAssetIssueInSimpleClass;
        TestAsset m_TestAssetShaderWarmupIssueIsCritical;

        [SerializeField]
        ReportItem m_Issue;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            m_TestAssetIssueInSimpleClass = new TestAsset("IssueInSimpleClass.cs", @"
using UnityEngine;
class IssueInSimpleClass
{
    void Dummy()
    {
        Debug.Log(Camera.allCameras.Length);
    }
}
");

            m_TestAssetIssueInMonoBehaviourUpdate = new TestAsset("IssueInMonoBehaviourUpdate.cs", @"
using UnityEngine;
class IssueInMonoBehaviourUpdate : MonoBehaviour
{
    void Update()
    {
        Debug.Log(Camera.allCameras.Length);
    }
}
");

            m_TestAssetIssueInMonoBehaviourOnAnimatorMove = new TestAsset("IssueInMonoBehaviourOnAnimatorMove.cs", @"
using UnityEngine;
class IssueInMonoBehaviourOnAnimatorMove : MonoBehaviour
{
    void OnAnimatorMove()
    {
        Debug.Log(Camera.allCameras.Length);
    }
}
");

            m_TestAssetIssueInMonoBehaviourOnRenderObject = new TestAsset("m_TempAssetIssueInMonoBehaviourOnRenderObject.cs", @"
using UnityEngine;
class IssueInMonoBehaviourOnObjectRender : MonoBehaviour
{
    void OnRenderObject()
    {
        Debug.Log(Camera.allCameras.Length);
    }
}
");

            m_TestAssetIssueInClassMethodCalledFromMonoBehaviourUpdate = new TestAsset(
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

            m_TestAssetIssueInClassInheritedFromMonoBehaviour = new TestAsset(
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

            m_TestAssetShaderWarmupIssueIsCritical = new TestAsset("ShaderWarmUpIssueIsCritical.cs", @"
using UnityEngine;
class ShaderWarmUpIssueIsCritical
{
    void Start()
    {
        Shader.WarmupAllShaders();
    }
}
");
            AnalyzeTestAssets();
        }

        [Test]
        public void CodeAnalysis_IssueInSimpleClass_IsSeverityCorrect()
        {
            var issues = GetIssuesForAsset(m_TestAssetIssueInSimpleClass);
            var issue = issues.First();
            Assert.AreEqual(Severity.Moderate, issue.Severity);
        }

        [Test]
        public void CodeAnalysis_IssueInMonoBehaviourUpdate_IsSeverityCorrect()
        {
            var issues = GetIssuesForAsset(m_TestAssetIssueInMonoBehaviourUpdate);
            var issue = issues.First();
            Assert.AreEqual(Severity.Major, issue.Severity);
        }

        [Test]
        public void CodeAnalysis_IssueInMonoBehaviourOnAnimatorMove_IsSeverityCorrect()
        {
            var issues = GetIssuesForAsset(m_TestAssetIssueInMonoBehaviourOnAnimatorMove);
            var issue = issues.First();
            Assert.AreEqual(Severity.Major, issue.Severity);
        }

        [Test]
        public void CodeAnalysis_MonoBehaviourOnRenderObject_IsSeverityCorrect()
        {
            var issues = GetIssuesForAsset(m_TestAssetIssueInMonoBehaviourOnRenderObject);
            var issue = issues.First();
            Assert.AreEqual(Severity.Major, issue.Severity);
        }

        [Test]
        public void CodeAnalysis_IssueInClassMethodCalledFromMonoBehaviourUpdate_IsSeverityCorrect()
        {
            var issues = GetIssuesForAsset(m_TestAssetIssueInClassMethodCalledFromMonoBehaviourUpdate);
            var issue = issues.First();
            Assert.AreEqual(Severity.Major, issue.Severity);
        }

        [Test]
        public void CodeAnalysis_IssueInClassInheritedFromMonoBehaviour_IsSeverityCorrect()
        {
            var issues = GetIssuesForAsset(m_TestAssetIssueInClassInheritedFromMonoBehaviour);
            var issue = issues.First();
            Assert.AreEqual(Severity.Major, issue.Severity);
        }

        [UnityTest]
        public IEnumerator CodeAnalysis_Critical_PersistsAfterDomainReload()
        {
            var issues = GetIssuesForAsset(m_TestAssetIssueInClassInheritedFromMonoBehaviour);
            m_Issue = issues.First();

            Assert.AreEqual(Severity.Major, m_Issue.Severity);

            EditorUtility.RequestScriptReload();
            yield return new WaitForDomainReload();

            Assert.AreEqual(Severity.Major, m_Issue.Severity);
        }

        [Test]
        public void CodeAnalysis_ShaderWarmupIssue_IsSeverityCorrect()
        {
            var issues = GetIssuesForAsset(m_TestAssetShaderWarmupIssueIsCritical);
            var issue = issues.First();
            Assert.AreEqual(Severity.Major, issue.Severity);
        }
    }
}
