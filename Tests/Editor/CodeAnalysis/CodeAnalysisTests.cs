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
    class CodeAnalysisTests : TestFixtureBase
    {
        TempAsset m_TempAsset;
        TempAsset m_TempAssetClassWithConditionalAttribute;
        TempAsset m_TempAssetDerivedClassMethod;
        TempAsset m_TempAssetInPlugin;
        TempAsset m_TempAssetIssueInCoroutine;
        TempAsset m_TempAssetIssueInDelegate;
        TempAsset m_TempAssetIssueInProperty;
        TempAsset m_TempAssetIssueInGenericClass;
        TempAsset m_TempAssetIssueInMonoBehaviour;
        TempAsset m_TempAssetIssueInNestedClass;
        TempAsset m_TempAssetIssueInOverrideMethod;
        TempAsset m_TempAssetIssueInVirtualMethod;
        TempAsset m_TempAssetAnyApiInNamespace;
        TempAsset m_TempAssetGenericInstantiation;

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

            m_TempAssetClassWithConditionalAttribute = new TempAsset("ClassWithConditionalAttribute.cs", @"
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
        Debug.Log(6); // boxing
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

            m_TempAssetIssueInProperty = new TempAsset("IssueInProperty.cs", @"
class IssueInProperty
{
    object property
    {
        get { return 6; }
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

            m_TempAssetGenericInstantiation = new TempAsset("GenericInstantiation.cs", @"
using System.Collections.Generic;
class GenericInstantiation
{
    HashSet<string> m_GenericInstance;
    void Dummy()
    {
        m_GenericInstance = new HashSet<string>();
    }
}
");
        }

        [Test]
        public void CodeAnalysis_Paths_CanBeResolved()
        {
            var issues = Analyze(i => i.category == IssueCategory.Code);
            foreach (var issue in issues)
            {
                var relativePath = issue.relativePath;

                Assert.False(string.IsNullOrEmpty(relativePath));
            }
        }

        [Test]
        public void CodeAnalysis_Issue_IsReported()
        {
            var issues = AnalyzeAndFindAssetIssues(m_TempAsset);

            Assert.AreEqual(1, issues.Count());

            var myIssue = issues.FirstOrDefault();

            Assert.NotNull(myIssue);
            Assert.NotNull(myIssue.descriptor);

            Assert.AreEqual(Rule.Severity.Default, myIssue.descriptor.severity);
            Assert.AreEqual(typeof(string), myIssue.descriptor.id.GetType());
            Assert.AreEqual("PAC0066", myIssue.descriptor.id);
            Assert.AreEqual("UnityEngine.Camera", myIssue.descriptor.type);
            Assert.AreEqual("allCameras", myIssue.descriptor.method);

            Assert.AreEqual(m_TempAsset.fileName, myIssue.filename);
            Assert.AreEqual("'UnityEngine.Camera.allCameras' usage", myIssue.description);
            Assert.AreEqual("System.Void MyClass::Dummy()", myIssue.GetContext());
            Assert.AreEqual(7, myIssue.line);
            Assert.AreEqual(IssueCategory.Code, myIssue.category);

            // check custom property
            Assert.AreEqual((int)CodeProperty.Num, myIssue.GetNumCustomProperties());
            Assert.AreEqual(AssemblyInfo.DefaultAssemblyName, myIssue.GetCustomProperty(CodeProperty.Assembly));
        }

        [Test]
        public void CodeAnalysis_ConditionalMethodCallSites_AreRemoved()
        {
            var issues = AnalyzeAndFindAssetIssues(m_TempAssetClassWithConditionalAttribute);
            Assert.Positive(issues.Length);
            Assert.NotNull(issues[0]);
            Assert.NotNull(issues[0].dependencies);

            // all call sites should be removed by the compiler
            Assert.False(issues[0].dependencies.HasChildren());
        }

        [Test]
        public void CodeAnalysis_DerivedClassMethodIssue_IsReported()
        {
            var filteredIssues = AnalyzeAndFindAssetIssues(m_TempAssetDerivedClassMethod);

            Assert.AreEqual(1, filteredIssues.Count());

            var myIssue = filteredIssues.FirstOrDefault();

            Assert.NotNull(myIssue);
            Assert.NotNull(myIssue.descriptor);
            Assert.AreEqual("'UnityEngine.Component.tag' usage", myIssue.description);
        }

        [Test]
        public void CodeAnalysis_IssueInPlugin_IsReported()
        {
            var issues = AnalyzeAndFindAssetIssues(m_TempAssetInPlugin);

            Assert.AreEqual(1, issues.Count());
            Assert.AreEqual("System.Void MyPlugin::Dummy()", issues[0].GetContext());
        }

        [Test]
        public void CodeAnalysis_IssueInNestedClass_IsReported()
        {
            var issues = AnalyzeAndFindAssetIssues(m_TempAssetIssueInNestedClass);

            Assert.AreEqual(1, issues.Count());
            Assert.AreEqual("System.Void MyClassWithNested/NestedClass::Dummy()", issues[0].GetContext());
        }

        [Test]
        public void CodeAnalysis_IssueInGenericClass_IsReported()
        {
            var issues = AnalyzeAndFindAssetIssues(m_TempAssetIssueInGenericClass);

            Assert.AreEqual(1, issues.Count());
            Assert.AreEqual("System.Void GenericClass`1::Dummy()", issues[0].GetContext());
        }

        [Test]
        public void CodeAnalysis_IssueInVirtualMethod_IsReported()
        {
            var issues = AnalyzeAndFindAssetIssues(m_TempAssetIssueInVirtualMethod);

            Assert.AreEqual(1, issues.Count());
            Assert.AreEqual("System.Void AbstractClass::Dummy()", issues[0].GetContext());
        }

        [Test]
        public void CodeAnalysis_IssueInOverrideMethod_IsReported()
        {
            var issues = AnalyzeAndFindAssetIssues(m_TempAssetIssueInOverrideMethod);

            Assert.AreEqual(1, issues.Count());
            Assert.AreEqual("System.Void DerivedClass::Dummy()", issues[0].GetContext());
        }

        [Test]
        public void CodeAnalysis_IssueInMonoBehaviour_IsReported()
        {
            var issues = AnalyzeAndFindAssetIssues(m_TempAssetIssueInMonoBehaviour);

            Assert.AreEqual(1, issues.Count());
            Assert.AreEqual("System.Void MyMonoBehaviour::Start()", issues[0].GetContext());
        }

        [Test]
        public void CodeAnalysis_IssueInCoroutine_IsReported()
        {
            var issues = AnalyzeAndFindAssetIssues(m_TempAssetIssueInCoroutine);

            Assert.AreEqual(1, issues.Count());
            Assert.AreEqual("System.Boolean MyMonoBehaviourWithCoroutine/<MyCoroutine>d__1::MoveNext()", issues[0].GetContext());
        }

        [Test]
        public void CodeAnalysis_IssueInDelegate_IsReported()
        {
            var allScriptIssues = AnalyzeAndFindAssetIssues(m_TempAssetIssueInDelegate);
            var issue = allScriptIssues.FirstOrDefault(i => i.description.Equals("'UnityEngine.Camera.allCameras' usage"));
            Assert.NotNull(issue);
            Assert.AreEqual("System.Int32 ClassWithDelegate/<>c::<Dummy>b__1_0()", issue.GetContext());
        }

        [Test]
        public void CodeAnalysis_IssueInProperty_IsReported()
        {
            var issues = AnalyzeAndFindAssetIssues(m_TempAssetIssueInProperty, IssueCategory.Code);

            Assert.AreEqual(1, issues.Length);
            Assert.AreEqual("Conversion from value type 'Int32' to ref type", issues[0].description);
            Assert.AreEqual("IssueInProperty.get_property", issues[0].dependencies.prettyName);
        }

        [Test]
        public void CodeAnalysis_IssueInNamespace_IsReported()
        {
            var allScriptIssues = AnalyzeAndFindAssetIssues(m_TempAssetAnyApiInNamespace);
            var issue = allScriptIssues.FirstOrDefault(i => i.description.Equals("'System.Linq.Enumerable.Sum' usage"));

            Assert.NotNull(issue);
            Assert.AreEqual("System.Linq.*", issue.descriptor.description);
        }

        [Test]
        public void CodeAnalysis_GenericInstantiation_IsReported()
        {
            var issues = AnalyzeAndFindAssetIssues(m_TempAssetGenericInstantiation, IssueCategory.GenericInstance);

            Assert.AreEqual(1, issues.Length);
            Assert.AreEqual("'System.Collections.Generic.HashSet`1<System.String>' generic instance", issues[0].description);
        }

        [Test]
        public void CodeAnalysis_DefaultAssembly_IsOnlyReportedAssembly()
        {
            var issues = Analyze(IssueCategory.Code);

            Assert.True(issues.All(i => i.GetCustomProperty(CodeProperty.Assembly).Equals(AssemblyInfo.DefaultAssemblyName)));
        }
    }
}
