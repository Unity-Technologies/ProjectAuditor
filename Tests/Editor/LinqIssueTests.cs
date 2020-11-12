using System;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;

namespace UnityEditor.ProjectAuditor.EditorTests
{
    class LinqIssueTests
    {
        ScriptResource m_ScriptResource;

        [SetUp]
        public void SetUp()
        {
            m_ScriptResource = new ScriptResource("MyClass.cs", @"
using System.Linq;
using System.Collections.Generic;

class MyClass
{
    int Dummy(List<int> list)
    {
        return list.Count();
    }
}"
            );
        }

        [TearDown]
        public void TearDown()
        {
            m_ScriptResource.Delete();
        }

        [Test]
        public void LinqIssueIsReported()
        {
            var issues = ScriptIssueTestHelper.AnalyzeAndFindScriptIssues(m_ScriptResource);

            Assert.AreEqual(1, issues.Count());

            var myIssue = issues.FirstOrDefault();

            Assert.NotNull(myIssue);
            Assert.NotNull(myIssue.descriptor);

            Assert.AreEqual(Rule.Action.Default, myIssue.descriptor.action);
            Assert.AreEqual(101049, myIssue.descriptor.id);
            Assert.True(myIssue.descriptor.type.Equals("System.Linq"));
            Assert.True(myIssue.descriptor.method.Equals("*"));

            Assert.True(myIssue.name.Equals("Enumerable.Count"));
            Assert.True(myIssue.filename.Equals(m_ScriptResource.scriptName));
            Assert.True(myIssue.description.Equals("Enumerable.Count"));
            Assert.True(
                myIssue.callingMethod.Equals(
                    "System.Int32 MyClass::Dummy(System.Collections.Generic.List`1<System.Int32>)"));
            Assert.AreEqual(9, myIssue.line);
            Assert.AreEqual(IssueCategory.Code, myIssue.category);
        }
    }
}
