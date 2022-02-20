using System;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Auditors;
using Unity.ProjectAuditor.Editor.CodeAnalysis;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEngine;

namespace Unity.ProjectAuditor.EditorTests
{
    class CodeAnalysisTests
    {
        TempAsset m_TempAsset;
        TempAsset m_TempAssetDerivedClassMethod;
        TempAsset m_TempAssetInPlugin;
        TempAsset m_TempAssetInEditorCode;
        TempAsset m_TempAssetInPlayerCode;
        TempAsset m_TempAssetIssueInCoroutine;
        TempAsset m_TempAssetIssueInDelegate;
        TempAsset m_TempAssetIssueInGenericClass;
        TempAsset m_TempAssetIssueInMonoBehaviour;
        TempAsset m_TempAssetIssueInNestedClass;
        TempAsset m_TempAssetIssueInOverrideMethod;
        TempAsset m_TempAssetIssueInVirtualMethod;
        TempAsset m_TempAssetAnyApiInNamespace;
        TempAsset m_TempAssetObjectName;

        [OneTimeSetUp]
        public void SetUp()
        {
            m_TempAsset = new TempAsset("MyClass.cs", @"
using UnityEngine;
class MyClass
{
    void Dummy()
    {
        Debug.Log(Camera.allCameras.Length.ToString());
    }
}
");

            m_TempAssetDerivedClassMethod = new TempAsset("DerivedClassMethod.cs", @"
using UnityEngine;
class DerivedClassMethod
{
    bool IsMainCamera(Camera camera)
    {
        return camera.tag == ""MainCamera"";
    }
}
");

            m_TempAssetInPlugin = new TempAsset("Plugins/MyPlugin.cs", @"
using UnityEngine;
class MyPlugin
{
    void Dummy()
    {
        Debug.Log(Camera.allCameras.Length.ToString());
    }
}
");

            m_TempAssetInPlayerCode = new TempAsset("IssueInPlayerCode.cs", @"
using UnityEngine;
class MyClassWithPlayerOnlyCode
{
    void Dummy()
    {
#if !UNITY_EDITOR
        Debug.Log(Camera.allCameras.Length.ToString());
#endif
    }
}
");

            m_TempAssetInEditorCode = new TempAsset("IssueInEditorCode.cs", @"
using UnityEngine;
class MyClassWithEditorOnlyCode
{
    void Dummy()
    {
#if UNITY_EDITOR
        Debug.Log(Camera.allCameras.Length.ToString());
#endif
    }
}
");

            m_TempAssetIssueInNestedClass = new TempAsset("IssueInNestedClass.cs", @"
using UnityEngine;
class MyClassWithNested
{
    class NestedClass
    {
        void Dummy()
        {
            Debug.Log(Camera.allCameras.Length.ToString());
        }
    }
}
");

            m_TempAssetIssueInGenericClass = new TempAsset("IssueInGenericClass.cs", @"
using UnityEngine;
class GenericClass<T>
{
    void Dummy()
    {
        Debug.Log(Camera.allCameras.Length.ToString());
    }
}
");

            m_TempAssetIssueInVirtualMethod = new TempAsset("IssueInVirtualMethod.cs", @"
using UnityEngine;
abstract class AbstractClass
{
    public virtual void Dummy()
    {
        Debug.Log(Camera.allCameras.Length.ToString());
    }
}
");

            m_TempAssetIssueInOverrideMethod = new TempAsset("IssueInOverrideMethod.cs", @"
using UnityEngine;
class BaseClass
{
    public virtual void Dummy()
    { }
}

class DerivedClass : BaseClass
{
    public override void Dummy()
    {
        Debug.Log(Camera.allCameras.Length.ToString());
    }
}
");

            m_TempAssetIssueInMonoBehaviour = new TempAsset("IssueInMonoBehaviour.cs", @"
using UnityEngine;
class MyMonoBehaviour : MonoBehaviour
{
    void Start()
    {
        Debug.Log(Camera.allCameras.Length.ToString());
    }
}
");

            m_TempAssetIssueInCoroutine = new TempAsset("IssueInCoroutine.cs", @"
using UnityEngine;
using System.Collections;
class MyMonoBehaviourWithCoroutine : MonoBehaviour
{
    void Start()
    {
        StartCoroutine(MyCoroutine());
    }

    IEnumerator MyCoroutine()
    {
        yield return 1;
    }
}
");

            m_TempAssetIssueInDelegate = new TempAsset("IssueInDelegate.cs", @"
using UnityEngine;
using System;
class ClassWithDelegate
{
    private Func<int> myFunc;

    void Dummy()
    {
        myFunc = () =>
        {
            Debug.Log(Camera.allCameras.Length.ToString());
            return 0;
        };
    }
}
");

            m_TempAssetAnyApiInNamespace = new TempAsset("AnyApiInNamespace.cs", @"
using System.Linq;
using System.Collections.Generic;
class AnyApiInNamespace
{
    int SumAllValues(List<int> list)
    {
        return list.Sum();
    }
}
");

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
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            TempAsset.Cleanup();
        }

        [Test]
        public void CodeAnalysis_Paths_CanBeResolved()
        {
            var issues = Utility.Analyze(i => i.category == IssueCategory.Code);
            foreach (var issue in issues)
            {
                var relativePath = issue.relativePath;

                Assert.False(string.IsNullOrEmpty(relativePath));
            }
        }

        [Test]
        public void CodeAnalysis_IssueInEditorCode_IsNotReported()
        {
            var config = ScriptableObject.CreateInstance<ProjectAuditorConfig>();
            config.AnalyzeEditorCode = false;

            var projectAuditor = new Unity.ProjectAuditor.Editor.ProjectAuditor(config);

            var projectReport = projectAuditor.Audit();
            var issues = projectReport.GetIssues(IssueCategory.Code);
            var codeIssue = issues.FirstOrDefault(i => i.relativePath.Equals(m_TempAssetInEditorCode.relativePath));

            Assert.Null(codeIssue);
        }

        [Test]
        [Ignore("Known failure because the script is not recompiled by the editor")]
        public void CodeAnalysis_IssueInEditorCode_IsReported()
        {
            var config = ScriptableObject.CreateInstance<ProjectAuditorConfig>();
            config.AnalyzeEditorCode = true;

            var projectAuditor = new Unity.ProjectAuditor.Editor.ProjectAuditor(config);
            var projectReport = projectAuditor.Audit();

            var issues = projectReport.GetIssues(IssueCategory.Code);
            var codeIssue = issues.FirstOrDefault(i => i.relativePath.Equals(m_TempAssetInEditorCode.relativePath));

            Assert.NotNull(codeIssue);
        }

        [Test]
        public void CodeAnalysis_Issue_IsReported()
        {
            var issues = Utility.AnalyzeAndFindAssetIssues(m_TempAsset);

            Assert.AreEqual(1, issues.Count());

            var myIssue = issues.FirstOrDefault();

            Assert.NotNull(myIssue);
            Assert.NotNull(myIssue.descriptor);

            Assert.AreEqual(Rule.Severity.Default, myIssue.descriptor.severity);
            Assert.AreEqual(101066, myIssue.descriptor.id);
            Assert.AreEqual("UnityEngine.Camera", myIssue.descriptor.type);
            Assert.AreEqual("allCameras", myIssue.descriptor.method);

            Assert.AreEqual("Camera.get_allCameras", myIssue.name);
            Assert.AreEqual(m_TempAsset.fileName, myIssue.filename);
            Assert.AreEqual("UnityEngine.Camera.allCameras", myIssue.description);
            Assert.AreEqual("System.Void MyClass::Dummy()", myIssue.GetCallingMethod());
            Assert.AreEqual(7, myIssue.line);
            Assert.AreEqual(IssueCategory.Code, myIssue.category);

            // check custom property
            Assert.AreEqual((int)CodeProperty.Num, myIssue.GetNumCustomProperties());
            Assert.AreEqual(AssemblyInfo.DefaultAssemblyName, myIssue.GetCustomProperty(CodeProperty.Assembly));
        }

        [Test]
        public void CodeAnalysis_DerivedClassMethodIssue_IsReported()
        {
            var filteredIssues = Utility.AnalyzeAndFindAssetIssues(m_TempAssetDerivedClassMethod);

            Assert.AreEqual(1, filteredIssues.Count());

            var myIssue = filteredIssues.FirstOrDefault();

            Assert.NotNull(myIssue);
            Assert.NotNull(myIssue.descriptor);
            Assert.AreEqual("UnityEngine.Component.tag", myIssue.description);
        }

        [Test]
        public void CodeAnalysis_IssueInPlugin_IsReported()
        {
            var issues = Utility.AnalyzeAndFindAssetIssues(m_TempAssetInPlugin);

            Assert.AreEqual(1, issues.Count());
            Assert.AreEqual("System.Void MyPlugin::Dummy()", issues.First().GetCallingMethod());
        }

        [Test]
        public void CodeAnalysis_IssueInPlayerCode_IsReported()
        {
            var issues = Utility.AnalyzeAndFindAssetIssues(m_TempAssetInPlayerCode);

            Assert.AreEqual(1, issues.Count());
            Assert.AreEqual("System.Void MyClassWithPlayerOnlyCode::Dummy()", issues.First().GetCallingMethod());
        }

        [Test]
        public void CodeAnalysis_IssueInNestedClass_IsReported()
        {
            var issues = Utility.AnalyzeAndFindAssetIssues(m_TempAssetIssueInNestedClass);

            Assert.AreEqual(1, issues.Count());
            Assert.AreEqual("System.Void MyClassWithNested/NestedClass::Dummy()", issues.First().GetCallingMethod());
        }

        [Test]
        public void CodeAnalysis_IssueInGenericClass_IsReported()
        {
            var issues = Utility.AnalyzeAndFindAssetIssues(m_TempAssetIssueInGenericClass);

            Assert.AreEqual(1, issues.Count());
            Assert.AreEqual("System.Void GenericClass`1::Dummy()", issues.First().GetCallingMethod());
        }

        [Test]
        public void CodeAnalysis_IssueInVirtualMethod_IsReported()
        {
            var issues = Utility.AnalyzeAndFindAssetIssues(m_TempAssetIssueInVirtualMethod);

            Assert.AreEqual(1, issues.Count());
            Assert.AreEqual("System.Void AbstractClass::Dummy()", issues.First().GetCallingMethod());
        }

        [Test]
        public void CodeAnalysis_IssueInOverrideMethod_IsReported()
        {
            var issues = Utility.AnalyzeAndFindAssetIssues(m_TempAssetIssueInOverrideMethod);

            Assert.AreEqual(1, issues.Count());
            Assert.AreEqual("System.Void DerivedClass::Dummy()", issues.First().GetCallingMethod());
        }

        [Test]
        public void CodeAnalysis_IssueInMonoBehaviour_IsReported()
        {
            var issues = Utility.AnalyzeAndFindAssetIssues(m_TempAssetIssueInMonoBehaviour);

            Assert.AreEqual(1, issues.Count());
            Assert.AreEqual("System.Void MyMonoBehaviour::Start()", issues.First().GetCallingMethod());
        }

        [Test]
        public void CodeAnalysis_IssueInCoroutine_IsReported()
        {
            var issues = Utility.AnalyzeAndFindAssetIssues(m_TempAssetIssueInCoroutine);

            Assert.AreEqual(1, issues.Count());
            Assert.AreEqual("System.Boolean MyMonoBehaviourWithCoroutine/<MyCoroutine>d__1::MoveNext()", issues.First().GetCallingMethod());
        }

        [Test]
        public void CodeAnalysis_IssueInDelegate_IsReported()
        {
            var allScriptIssues = Utility.AnalyzeAndFindAssetIssues(m_TempAssetIssueInDelegate);
            var issue = allScriptIssues.FirstOrDefault(i => i.name.Equals("Camera.get_allCameras"));
            Assert.NotNull(issue);
            Assert.AreEqual("System.Int32 ClassWithDelegate/<>c::<Dummy>b__1_0()", issue.GetCallingMethod());
        }

        [Test]
        public void CodeAnalysis_IssueInNamespace_IsReported()
        {
            var allScriptIssues = Utility.AnalyzeAndFindAssetIssues(m_TempAssetAnyApiInNamespace);
            var issue = allScriptIssues.FirstOrDefault(i => i.description.Equals("System.Linq.Enumerable.Sum"));

            Assert.NotNull(issue);
            Assert.AreEqual("System.Linq.*", issue.descriptor.description);
        }

        [Test]
        public void CodeAnalysis_ObjectName_IsReported()
        {
            var issues = Utility.AnalyzeAndFindAssetIssues(m_TempAssetObjectName);

            Assert.AreEqual(3, issues.Length);
            Assert.True(issues.All(i => i.name.Equals("Object.get_name")));
            Assert.True(issues.All(i => i.description.Equals("UnityEngine.Object.name")));
        }
    }
}
