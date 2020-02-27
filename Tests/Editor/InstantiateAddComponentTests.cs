using System.Linq;
using NUnit.Framework;

namespace UnityEditor.ProjectAuditor.EditorTests
{
    public class InstantiateAddComponentTests
    {
        private ScriptResource m_ScriptResourceInstantiate;
        private ScriptResource m_ScriptResourceAddComponent;
        
        [OneTimeSetUp]
        public void SetUp()
        {
             m_ScriptResourceInstantiate = new ScriptResource("InstantiateObject.cs", @"
using UnityEngine;
class InstantiateObject : MonoBehaviour
{
	public GameObject Prefab;
	public GameObject Instance;
	void Start()
	{
        Instance = Instantiate(Prefab);
	}
}
");

             m_ScriptResourceAddComponent = new ScriptResource("AddComponentToGameObject.cs", @"
using UnityEngine;
class AddComponentToGameObject : MonoBehaviour
{
	public GameObject Instance;
	void Start()
	{
		Instance.AddComponent<Rigidbody>();
	}
}
");
        }

        [OneTimeTearDown]
        public void TearDown()
        {
	        m_ScriptResourceInstantiate.Delete();
	        m_ScriptResourceAddComponent.Delete();
        }
        
        [Test]
        public void InstantiateIssueIsFound()
        {
            var issues = ScriptIssueTestHelper.AnalyzeAndFindScriptIssues(m_ScriptResourceInstantiate);
			
            Assert.AreEqual(1, issues.Count());
						
            Assert.True(issues.First().callingMethod.Equals("System.Void InstantiateObject::Start()"));
        }
		
        [Test]
        public void AddComponentIssueIsFound()
        {
            var issues = ScriptIssueTestHelper.AnalyzeAndFindScriptIssues(m_ScriptResourceAddComponent);
			
            Assert.AreEqual(1, issues.Count());
						
            Assert.True(issues.First().callingMethod.Equals("System.Void AddComponentToGameObject::Start()"));
        }
    }
}