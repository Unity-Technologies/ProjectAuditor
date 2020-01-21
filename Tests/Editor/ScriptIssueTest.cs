using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using UnityEngine;

namespace UnityEditor.ProjectAuditor.EditorTests
{
	class ScriptIssueTest
	{
		private ScriptResource m_ScriptResource;
		private ScriptResource m_ScriptResourceInPlayerCode;
		private ScriptResource m_ScriptResourceInEditorCode;
		private ScriptResource m_ScriptResourceIssueInNestedClass;
		private ScriptResource m_ScriptResourceIssueInGenericClass;
		private ScriptResource m_ScriptResourceIssueInMonoBehaviour;
		private ScriptResource m_ScriptResourceIssueInVirtualMethod;
		private ScriptResource m_ScriptResourceIssueInOverrideMethod;
		private ScriptResource m_ScriptResourceIssueInCoroutine;
		private ScriptResource m_ScriptResourceIssueInDelegate;
		
		[OneTimeSetUp]
		public void SetUp()
		{
			 m_ScriptResource = new ScriptResource("MyClass.cs", @"
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

			 m_ScriptResourceInPlayerCode = new ScriptResource("IssueInPlayerCode.cs", @"
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

			 m_ScriptResourceInEditorCode = new ScriptResource("IssueInEditorCode.cs", @"
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
			 
			 m_ScriptResourceIssueInNestedClass = new ScriptResource("IssueInNestedClass.cs", @"
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

			 m_ScriptResourceIssueInGenericClass = new ScriptResource("IssueInGenericClass.cs", @"
using UnityEngine;
class GenericClass<T>
{
	void Dummy()
	{
		Debug.Log(Camera.main.name);
	}
}
");
			 
			 m_ScriptResourceIssueInVirtualMethod = new ScriptResource("IssueInVirtualMethod.cs", @"
using UnityEngine;
abstract class AbstractClass
{
	public virtual void Dummy()
    {
		Debug.Log(Camera.main.name);
    }
}
");
			 
			 m_ScriptResourceIssueInOverrideMethod = new ScriptResource("IssueInOverrideMethod.cs", @"
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
			 
			 m_ScriptResourceIssueInMonoBehaviour = new ScriptResource("IssueInMonoBehaviour.cs", @"
using UnityEngine;
class MyMonoBehaviour : MonoBehaviour
{
	void Start()
	{
		Debug.Log(Camera.main.name);
	}
}
");

			 m_ScriptResourceIssueInCoroutine = new ScriptResource("IssueInCoroutine.cs", @"
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
			 
			 m_ScriptResourceIssueInDelegate = new ScriptResource("IssueInDelegate.cs", @"
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
			m_ScriptResource.Delete();
			m_ScriptResourceInPlayerCode.Delete();
			m_ScriptResourceInEditorCode.Delete();
			m_ScriptResourceIssueInNestedClass.Delete();
			m_ScriptResourceIssueInGenericClass.Delete();
			m_ScriptResourceIssueInVirtualMethod.Delete();
			m_ScriptResourceIssueInOverrideMethod.Delete();
			m_ScriptResourceIssueInMonoBehaviour.Delete();
			m_ScriptResourceIssueInCoroutine.Delete();
			m_ScriptResourceIssueInDelegate.Delete();
		}

		[Test]
		public void IssueIsFound()
		{
			var issues = ScriptIssueTestHelper.AnalyzeAndFindScriptIssues(m_ScriptResource);
			
			Assert.AreEqual(1, issues.Count());

			var myIssue = issues.FirstOrDefault();
			
			Assert.NotNull(myIssue);
			Assert.NotNull(myIssue.descriptor);
			
			Assert.AreEqual(Rule.Action.Default, myIssue.descriptor.action);
			Assert.AreEqual(101000, myIssue.descriptor.id);
			Assert.True(myIssue.descriptor.type.Equals("UnityEngine.Camera"));
			Assert.True(myIssue.descriptor.method.Equals("main"));
			
			Assert.True(myIssue.name.Equals("Camera.get_main"));
			Assert.True(myIssue.filename.Equals(m_ScriptResource.scriptName));
			Assert.True(myIssue.description.Equals("UnityEngine.Camera.main"));
			Assert.True(myIssue.callingMethod.Equals("System.Void MyClass::Dummy()"));
			Assert.AreEqual(8, myIssue.line);
			Assert.AreEqual(IssueCategory.ApiCalls, myIssue.category);
		}

		[Test]
		public void IssueInPlayerCodeIsFound()
		{
			var issues = ScriptIssueTestHelper.AnalyzeAndFindScriptIssues(m_ScriptResourceInPlayerCode);
			
			Assert.AreEqual(1, issues.Count());
						
			Assert.True(issues.First().callingMethod.Equals("System.Void MyClassWithPlayerOnlyCode::Dummy()"));
		}

		[Test]
		public void IssueInEditorCodeIsNotFound()
		{
			var issues = ScriptIssueTestHelper.AnalyzeAndFindScriptIssues(m_ScriptResourceInEditorCode);
			
			Assert.AreEqual(0, issues.Count());
		}

		[Test]
		public void IssueInNestedClassIsFound()
		{
			var issues = ScriptIssueTestHelper.AnalyzeAndFindScriptIssues(m_ScriptResourceIssueInNestedClass);
			
			Assert.AreEqual(1, issues.Count());
						
			Assert.True(issues.First().callingMethod.Equals("System.Void MyClassWithNested/NestedClass::Dummy()"));
		}

		[Test]
		public void IssueInGenericClassIsFound()
		{
				var issues = ScriptIssueTestHelper.AnalyzeAndFindScriptIssues(m_ScriptResourceIssueInGenericClass);
			
			Assert.AreEqual(1, issues.Count());
						
			Assert.True(issues.First().callingMethod.Equals("System.Void GenericClass`1::Dummy()"));
		}
		
		[Test]
		public void IssueInVirtualMethodIsFound()
		{
			var issues = ScriptIssueTestHelper.AnalyzeAndFindScriptIssues(m_ScriptResourceIssueInVirtualMethod);
			
			Assert.AreEqual(1, issues.Count());
						
			Assert.True(issues.First().callingMethod.Equals("System.Void AbstractClass::Dummy()"));
		}

		[Test]
		public void IssueInOverrideMethodIsFound()
		{
			var issues = ScriptIssueTestHelper.AnalyzeAndFindScriptIssues(m_ScriptResourceIssueInOverrideMethod);
			
			Assert.AreEqual(1, issues.Count());
						
			Assert.True(issues.First().callingMethod.Equals("System.Void DerivedClass::Dummy()"));
		}

		[Test]
		public void IssueInMonoBehaviourIsFound()
		{
			var issues = ScriptIssueTestHelper.AnalyzeAndFindScriptIssues(m_ScriptResourceIssueInMonoBehaviour);
			
			Assert.AreEqual(1, issues.Count());
						
			Assert.True(issues.First().callingMethod.Equals("System.Void MyMonoBehaviour::Start()"));
		}

		[Test]
		public void IssueInCoroutineIsFound()
		{
			var issues = ScriptIssueTestHelper.AnalyzeAndFindScriptIssues(m_ScriptResourceIssueInCoroutine);
			
			Assert.AreEqual(1, issues.Count());
						
			Assert.True(issues.First().callingMethod.Equals("System.Boolean MyMonoBehaviourWithCoroutine/<MyCoroutine>d__1::MoveNext()"));
		}
		
		[Test]
		public void IssueInDelegateIsFound()
		{
			var issues = ScriptIssueTestHelper.AnalyzeAndFindScriptIssues(m_ScriptResourceIssueInDelegate);
			
			Assert.AreEqual(1, issues.Count());
						
			Assert.True(issues.First().callingMethod.Equals("System.Int32 ClassWithDelegate/<>c::<Dummy>b__1_0()"));
		}
	}	
}

