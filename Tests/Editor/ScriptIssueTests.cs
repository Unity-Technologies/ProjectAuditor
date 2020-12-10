using System;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;

namespace UnityEditor.ProjectAuditor.EditorTests
{
    class ScriptIssueTests
    {
        TempAsset m_TempAsset;
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

        [OneTimeSetUp]
        public void SetUp()
        {
            m_TempAsset = new TempAsset("MyClass.cs", @"
using UnityEngine;
class MyClass
{
    void Dummy()
    {
        // Accessing Camera.main property is not recommended and will be reported as a possible performance problem.
        Debug.Log(Camera.main.name);
    }
}
");

            m_TempAssetInPlugin = new TempAsset("Plugins/MyPlugin.cs", @"
using UnityEngine;
class MyPlugin
{
    void Dummy()
    {
        // Accessing Camera.main property is not recommended and will be reported as a possible performance problem.
        Debug.Log(Camera.main.name);
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
        Debug.Log(Camera.main.name);
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
        Debug.Log(Camera.main.name);
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
            Debug.Log(Camera.main.name);
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
        Debug.Log(Camera.main.name);
    }
}
");

            m_TempAssetIssueInVirtualMethod = new TempAsset("IssueInVirtualMethod.cs", @"
using UnityEngine;
abstract class AbstractClass
{
    public virtual void Dummy()
    {
        Debug.Log(Camera.main.name);
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
        Debug.Log(Camera.main.name);
    }
}
");

            m_TempAssetIssueInMonoBehaviour = new TempAsset("IssueInMonoBehaviour.cs", @"
using UnityEngine;
class MyMonoBehaviour : MonoBehaviour
{
    void Start()
    {
        Debug.Log(Camera.main.name);
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
            Debug.Log(Camera.main.name);
            return 0;
        };
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
        public void IssueIsFound()
        {
            var issues = ScriptIssueTestHelper.AnalyzeAndFindScriptIssues(m_TempAsset);

            Assert.AreEqual(1, issues.Count());

            var myIssue = issues.FirstOrDefault();

            Assert.NotNull(myIssue);
            Assert.NotNull(myIssue.descriptor);

            Assert.AreEqual(Rule.Action.Default, myIssue.descriptor.action);
            Assert.AreEqual(101000, myIssue.descriptor.id);
            Assert.True(myIssue.descriptor.type.Equals("UnityEngine.Camera"));
            Assert.True(myIssue.descriptor.method.Equals("main"));

            Assert.True(myIssue.name.Equals("Camera.get_main"));
            Assert.True(myIssue.filename.Equals(m_TempAsset.scriptName));
            Assert.True(myIssue.description.Equals("UnityEngine.Camera.main"));
            Assert.True(myIssue.callingMethod.Equals("System.Void MyClass::Dummy()"));
            Assert.AreEqual(8, myIssue.line);
            Assert.AreEqual(IssueCategory.Code, myIssue.category);
        }

        [Test]
        public void IssueInPluginIsFound()
        {
            var issues = ScriptIssueTestHelper.AnalyzeAndFindScriptIssues(m_TempAssetInPlugin);

            Assert.AreEqual(1, issues.Count());

            Assert.True(issues.First().callingMethod.Equals("System.Void MyPlugin::Dummy()"));
        }

        [Test]
        public void IssueInPlayerCodeIsFound()
        {
            var issues = ScriptIssueTestHelper.AnalyzeAndFindScriptIssues(m_TempAssetInPlayerCode);

            Assert.AreEqual(1, issues.Count());

            Assert.True(issues.First().callingMethod.Equals("System.Void MyClassWithPlayerOnlyCode::Dummy()"));
        }

        [Test]
        public void IssueInEditorCodeIsNotFound()
        {
            var issues = ScriptIssueTestHelper.AnalyzeAndFindScriptIssues(m_TempAssetInEditorCode);

            Assert.AreEqual(0, issues.Count());
        }

        [Test]
        public void IssueInNestedClassIsFound()
        {
            var issues = ScriptIssueTestHelper.AnalyzeAndFindScriptIssues(m_TempAssetIssueInNestedClass);

            Assert.AreEqual(1, issues.Count());

            Assert.True(issues.First().callingMethod.Equals("System.Void MyClassWithNested/NestedClass::Dummy()"));
        }

        [Test]
        public void IssueInGenericClassIsFound()
        {
            var issues = ScriptIssueTestHelper.AnalyzeAndFindScriptIssues(m_TempAssetIssueInGenericClass);

            Assert.AreEqual(1, issues.Count());

            Assert.True(issues.First().callingMethod.Equals("System.Void GenericClass`1::Dummy()"));
        }

        [Test]
        public void IssueInVirtualMethodIsFound()
        {
            var issues = ScriptIssueTestHelper.AnalyzeAndFindScriptIssues(m_TempAssetIssueInVirtualMethod);

            Assert.AreEqual(1, issues.Count());

            Assert.True(issues.First().callingMethod.Equals("System.Void AbstractClass::Dummy()"));
        }

        [Test]
        public void IssueInOverrideMethodIsFound()
        {
            var issues = ScriptIssueTestHelper.AnalyzeAndFindScriptIssues(m_TempAssetIssueInOverrideMethod);

            Assert.AreEqual(1, issues.Count());

            Assert.True(issues.First().callingMethod.Equals("System.Void DerivedClass::Dummy()"));
        }

        [Test]
        public void IssueInMonoBehaviourIsFound()
        {
            var issues = ScriptIssueTestHelper.AnalyzeAndFindScriptIssues(m_TempAssetIssueInMonoBehaviour);

            Assert.AreEqual(1, issues.Count());

            Assert.True(issues.First().callingMethod.Equals("System.Void MyMonoBehaviour::Start()"));
        }

        [Test]
        public void IssueInCoroutineIsFound()
        {
            var issues = ScriptIssueTestHelper.AnalyzeAndFindScriptIssues(m_TempAssetIssueInCoroutine);

            Assert.AreEqual(1, issues.Count());

            Assert.True(issues.First().callingMethod
                .Equals("System.Boolean MyMonoBehaviourWithCoroutine/<MyCoroutine>d__1::MoveNext()"));
        }

        [Test]
        public void IssueInDelegateIsFound()
        {
            var allScriptIssues = ScriptIssueTestHelper.AnalyzeAndFindScriptIssues(m_TempAssetIssueInDelegate);
            var issue = allScriptIssues.FirstOrDefault(i => i.name.Equals("Camera.get_main"));
            Assert.NotNull(issue);

            Assert.True(issue.callingMethod.Equals("System.Int32 ClassWithDelegate/<>c::<Dummy>b__1_0()"));
        }
    }
}
