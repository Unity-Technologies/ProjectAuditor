using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.AssemblyUtils;
using Unity.ProjectAuditor.Editor.Modules;
using Unity.ProjectAuditor.Editor.Tests.Common;

namespace Unity.ProjectAuditor.EditorTests
{
    class CodeAnalysisTests : TestFixtureBase
    {
        TestAsset m_TestAsset;
        TestAsset m_TestAssetClassWithConditionalAttribute;
        TestAsset m_TestAssetDerivedClassMethod;
        TestAsset m_TestAssetInPlugin;
        TestAsset m_TestAssetIssueInCoroutine;
        TestAsset m_TestAssetIssueInDelegate;
        TestAsset m_TestAssetIssueInProperty;
        TestAsset m_TestAssetIssueInGenericClass;
        TestAsset m_TestAssetIssueInMonoBehaviour;
        TestAsset m_TestAssetIssueInNestedClass;
        TestAsset m_TestAssetIssueInOverrideMethod;
        TestAsset m_TestAssetIssueInVirtualMethod;
        TestAsset m_TestAssetAnyApiInNamespace;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            m_TestAsset = new TestAsset("MyClass.cs", @"
using UnityEngine;
class MyClass
{
    void Dummy()
    {
        Debug.LogError(Camera.allCameras.Length.ToString());
    }
}
");

            m_TestAssetClassWithConditionalAttribute = new TestAsset("ClassWithConditionalAttribute.cs", @"
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

class ClassWithConditionalAttribute
{
    void Caller()
    {
        // this call will be removed by the compiler
        MethodWithConditionalAttribute();
    }

    [Conditional(""ENABLE_LOG_NOT_DEFINED"")]
    void MethodWithConditionalAttribute()
    {
        Debug.LogError(6); // boxing
    }
}
");

            m_TestAssetDerivedClassMethod = new TestAsset("DerivedClassMethod.cs", @"
using UnityEngine;
class DerivedClassMethod
{
    bool IsMainCamera(Camera camera)
    {
        return camera.tag == ""MainCamera"";
    }
}
");

            m_TestAssetInPlugin = new TestAsset("Plugins/MyPlugin.cs", @"
using UnityEngine;
class MyPlugin
{
    void Dummy()
    {
        Debug.LogError(Camera.allCameras.Length.ToString());
    }
}
");

            m_TestAssetIssueInNestedClass = new TestAsset("IssueInNestedClass.cs", @"
using UnityEngine;
class MyClassWithNested
{
    class NestedClass
    {
        void Dummy()
        {
            Debug.LogError(Camera.allCameras.Length.ToString());
        }
    }
}
");

            m_TestAssetIssueInGenericClass = new TestAsset("IssueInGenericClass.cs", @"
using UnityEngine;
class GenericClass<T>
{
    void Dummy()
    {
        Debug.LogError(Camera.allCameras.Length.ToString());
    }
}
");

            m_TestAssetIssueInVirtualMethod = new TestAsset("IssueInVirtualMethod.cs", @"
using UnityEngine;
abstract class AbstractClass
{
    public virtual void Dummy()
    {
        Debug.LogError(Camera.allCameras.Length.ToString());
    }
}
");

            m_TestAssetIssueInOverrideMethod = new TestAsset("IssueInOverrideMethod.cs", @"
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
        Debug.LogError(Camera.allCameras.Length.ToString());
    }
}
");

            m_TestAssetIssueInMonoBehaviour = new TestAsset("IssueInMonoBehaviour.cs", @"
using UnityEngine;
class MyMonoBehaviour : MonoBehaviour
{
    void Start()
    {
        Debug.LogError(Camera.allCameras.Length.ToString());
    }
}
");

            m_TestAssetIssueInCoroutine = new TestAsset("IssueInCoroutine.cs", @"
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

            m_TestAssetIssueInDelegate = new TestAsset("IssueInDelegate.cs", @"
using UnityEngine;
using System;
class ClassWithDelegate
{
    private Func<int> myFunc;

    void Dummy()
    {
        myFunc = () =>
        {
            Debug.LogError(Camera.allCameras.Length.ToString());
            return 0;
        };
    }
}
");

            m_TestAssetIssueInProperty = new TestAsset("IssueInProperty.cs", @"
class IssueInProperty
{
    object property
    {
        get { return 6; }
    }
}
 ");

            m_TestAssetAnyApiInNamespace = new TestAsset("AnyApiInNamespace.cs", @"
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
            AnalyzeTestAssets();
        }

        [Test]
        public void CodeAnalysis_Paths_CanBeResolved()
        {
            foreach (var issue in GetIssues().Where(i => i.Category == IssueCategory.Code))
            {
                var relativePath = issue.RelativePath;

                Assert.False(string.IsNullOrEmpty(relativePath));
            }
        }

        [Test]
        public void CodeAnalysis_Issue_IsReported()
        {
            var issues = GetIssuesForAsset(m_TestAsset);

            Assert.AreEqual(1, issues.Count());

            var myIssue = issues.FirstOrDefault();

            Assert.NotNull(myIssue);
            var descriptor = myIssue.Id.GetDescriptor();

            Assert.AreEqual(Severity.Moderate, descriptor.DefaultSeverity);
            Assert.AreEqual(typeof(DescriptorId), myIssue.Id.GetType());
            Assert.AreEqual("PAC0066", myIssue.Id.ToString());
            Assert.AreEqual("UnityEngine.Camera", descriptor.Type);
            Assert.AreEqual("allCameras", descriptor.Method);

            Assert.AreEqual(m_TestAsset.FileName, myIssue.Filename);
            Assert.AreEqual("'UnityEngine.Camera.allCameras' usage", myIssue.Description);
            Assert.AreEqual("System.Void MyClass::Dummy()", myIssue.GetContext());
            Assert.AreEqual(7, myIssue.Line);
            Assert.AreEqual(IssueCategory.Code, myIssue.Category);

            // check custom property
            Assert.AreEqual((int)CodeProperty.Num, myIssue.GetNumCustomProperties());
            Assert.AreEqual(AssemblyInfo.DefaultAssemblyName, myIssue.GetCustomProperty(CodeProperty.Assembly));
        }

        [Test]
        public void CodeAnalysis_ConditionalMethodCallSites_AreRemoved()
        {
            var issues = GetIssuesForAsset(m_TestAssetClassWithConditionalAttribute);
            Assert.Positive(issues.Length);
            Assert.NotNull(issues[0]);
            Assert.NotNull(issues[0].Dependencies);

            // all call sites should be removed by the compiler
            Assert.False(issues[0].Dependencies.HasChildren);
        }

        [Test]
        public void CodeAnalysis_DerivedClassMethodIssue_IsReported()
        {
            var filteredIssues = GetIssuesForAsset(m_TestAssetDerivedClassMethod);

            Assert.AreEqual(1, filteredIssues.Count());

            var myIssue = filteredIssues.FirstOrDefault();

            Assert.NotNull(myIssue);
            Assert.IsTrue(myIssue.Id.IsValid());
            Assert.AreEqual("'UnityEngine.Component.tag' usage", myIssue.Description);
        }

        [Test]
        public void CodeAnalysis_IssueInPlugin_IsReported()
        {
            var issues = AnalyzeAndFindAssetIssues(m_TestAssetInPlugin);

            Assert.AreEqual(1, issues.Count());
            Assert.AreEqual("System.Void MyPlugin::Dummy()", issues[0].GetContext());
        }

        [Test]
        public void CodeAnalysis_IssueInNestedClass_IsReported()
        {
            var issues = GetIssuesForAsset(m_TestAssetIssueInNestedClass);

            Assert.AreEqual(1, issues.Count());
            Assert.AreEqual("System.Void MyClassWithNested/NestedClass::Dummy()", issues[0].GetContext());
        }

        [Test]
        public void CodeAnalysis_IssueInGenericClass_IsReported()
        {
            var issues = GetIssuesForAsset(m_TestAssetIssueInGenericClass);

            Assert.AreEqual(1, issues.Count());
            Assert.AreEqual("System.Void GenericClass`1::Dummy()", issues[0].GetContext());
        }

        [Test]
        public void CodeAnalysis_IssueInVirtualMethod_IsReported()
        {
            var issues = GetIssuesForAsset(m_TestAssetIssueInVirtualMethod);

            Assert.AreEqual(1, issues.Count());
            Assert.AreEqual("System.Void AbstractClass::Dummy()", issues[0].GetContext());
        }

        [Test]
        public void CodeAnalysis_IssueInOverrideMethod_IsReported()
        {
            var issues = GetIssuesForAsset(m_TestAssetIssueInOverrideMethod);

            Assert.AreEqual(1, issues.Count());
            Assert.AreEqual("System.Void DerivedClass::Dummy()", issues[0].GetContext());
        }

        [Test]
        public void CodeAnalysis_IssueInMonoBehaviour_IsReported()
        {
            var issues = GetIssuesForAsset(m_TestAssetIssueInMonoBehaviour);

            Assert.AreEqual(1, issues.Count());
            Assert.AreEqual("System.Void MyMonoBehaviour::Start()", issues[0].GetContext());
        }

        [Test]
        public void CodeAnalysis_IssueInCoroutine_IsReported()
        {
            var issues = GetIssuesForAsset(m_TestAssetIssueInCoroutine);

            Assert.AreEqual(1, issues.Count());
            Assert.AreEqual("System.Boolean MyMonoBehaviourWithCoroutine/<MyCoroutine>d__1::MoveNext()", issues[0].GetContext());
        }

        [Test]
        public void CodeAnalysis_IssueInDelegate_IsReported()
        {
            var allScriptIssues = GetIssuesForAsset(m_TestAssetIssueInDelegate);
            var issue = allScriptIssues.FirstOrDefault(i => i.Description.Equals("'UnityEngine.Camera.allCameras' usage"));
            Assert.NotNull(issue);
            Assert.AreEqual("System.Int32 ClassWithDelegate/<>c::<Dummy>b__1_0()", issue.GetContext());
        }

        [Test]
        public void CodeAnalysis_IssueInProperty_IsReported()
        {
            var issues = GetIssuesForAsset(m_TestAssetIssueInProperty);

            Assert.AreEqual(1, issues.Length);
            Assert.AreEqual("Conversion from value type 'Int32' to ref type", issues[0].Description);
            Assert.AreEqual("IssueInProperty.get_property", issues[0].Dependencies.PrettyName);
        }

        [Test]
        public void CodeAnalysis_IssueInNamespace_IsReported()
        {
            var allScriptIssues = GetIssuesForAsset(m_TestAssetAnyApiInNamespace);
            var issue = allScriptIssues.FirstOrDefault(i => i.Description.Equals("'System.Linq.Enumerable.Sum' usage"));

            Assert.NotNull(issue);
            var descriptor = issue.Id.GetDescriptor();
            Assert.AreEqual("System.Linq.*", descriptor.Title);
        }

        [Test]
        public void CodeAnalysis_DefaultAssembly_IsOnlyReportedAssembly()
        {
            Assert.True(GetIssues().Where(i => i.Category == IssueCategory.Code).All(i => i.GetCustomProperty(CodeProperty.Assembly).Equals(AssemblyInfo.DefaultAssemblyName)));
        }
    }
}
