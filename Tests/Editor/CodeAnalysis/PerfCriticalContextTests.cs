using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.ProjectAuditor.EditorTests
{
    [Serializable]
    class PerfCriticalContextTests : TestFixtureBase
    {
        TempAsset m_TempAssetIssueInClassInheritedFromMonoBehaviour;
        TempAsset m_TempAssetIssueInClassMethodCalledFromMonoBehaviourUpdate;
        TempAsset m_TempAssetIssueInMonoBehaviourUpdate;
        TempAsset m_TempAssetIssueInMonoBehaviourOnAnimatorMove;
        TempAsset m_TempAssetIssueInMonoBehaviourOnRenderObject;
        TempAsset m_TempAssetIssueInSimpleClass;
        TempAsset m_TempAssetShaderWarmupIssueIsCritical;

        [SerializeField]
        ProjectIssue m_Issue;

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

            m_TempAssetIssueInMonoBehaviourOnAnimatorMove = new TempAsset("IssueInMonoBehaviourOnAnimatorMove.cs", @"
using UnityEngine;
class IssueInMonoBehaviourOnAnimatorMove : MonoBehaviour
{
    void OnAnimatorMove()
    {
        Debug.Log(Camera.allCameras.Length);
    }
}
");

            m_TempAssetIssueInMonoBehaviourOnRenderObject = new TempAsset("m_TempAssetIssueInMonoBehaviourOnRenderObject.cs", @"
using UnityEngine;
class IssueInMonoBehaviourOnObjectRender : MonoBehaviour
{
    void OnRenderObject()
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

        [Test]
        public void CodeAnalysis_IssueInSimpleClass_IsNotCritical()
        {
            var issues = AnalyzeAndFindAssetIssues(m_TempAssetIssueInSimpleClass);
            var issue = issues.First();
            Assert.False(issue.isCritical);
        }

        [Test]
        public void CodeAnalysis_IssueInMonoBehaviourUpdate_IsCritical()
        {
            var issues = AnalyzeAndFindAssetIssues(m_TempAssetIssueInMonoBehaviourUpdate);
            var issue = issues.First();
            Assert.True(issue.isCritical);
        }

        [Test]
        public void CodeAnalysis_IssueInMonoBehaviourOnAnimatorMove_IsCritical()
        {
            var issues = AnalyzeAndFindAssetIssues(m_TempAssetIssueInMonoBehaviourOnAnimatorMove);
            var issue = issues.First();
            Assert.True(issue.isCritical);
        }

        [Test]
        public void CodeAnalysis_MonoBehaviourOnRenderObject_IsCriticalContext()
        {
            var issues = AnalyzeAndFindAssetIssues(m_TempAssetIssueInMonoBehaviourOnRenderObject);
            var issue = issues.First();
            Assert.True(issue.isCritical);
        }

        [Test]
        public void CodeAnalysis_IssueInClassMethodCalledFromMonoBehaviourUpdate_IsCritical()
        {
            var issues =
                AnalyzeAndFindAssetIssues(
                    m_TempAssetIssueInClassMethodCalledFromMonoBehaviourUpdate);
            var issue = issues.First();
            Assert.True(issue.isCritical);
        }

        [Test]
        public void CodeAnalysis_IssueInClassInheritedFromMonoBehaviour_IsCritical()
        {
            var issues =
                AnalyzeAndFindAssetIssues(
                    m_TempAssetIssueInClassInheritedFromMonoBehaviour);
            var issue = issues.First();
            Assert.True(issue.isCritical);
        }

        [UnityTest]
        public IEnumerator CodeAnalysis_Critical_PersistsAfterDomainReload()
        {
            var issues =
                AnalyzeAndFindAssetIssues(
                    m_TempAssetIssueInClassInheritedFromMonoBehaviour);
            m_Issue = issues.First();

            Assert.IsTrue(m_Issue.isCritical);
#if UNITY_2019_3_OR_NEWER
            EditorUtility.RequestScriptReload();
            yield return new WaitForDomainReload();

            Assert.IsTrue(m_Issue.isCritical);
#else
            yield return null;
#endif
        }

        [Test]
        public void CodeAnalysis_ShaderWarmupIssue_IsCritical()
        {
            var issues =
                AnalyzeAndFindAssetIssues(
                    m_TempAssetShaderWarmupIssueIsCritical);
            var issue = issues.First();
            Assert.True(issue.isCritical);
        }
    }
}
