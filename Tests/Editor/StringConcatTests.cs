using System;
using System.Linq;
using NUnit.Framework;

namespace UnityEditor.ProjectAuditor.EditorTests
{
    public class StringConcatTests
    {
        ScriptResource m_ScriptResourceStringConcat;

        [OneTimeSetUp]
        public void SetUp()
        {
            m_ScriptResourceStringConcat = new ScriptResource("StringConcat.cs", @"
class StringConcat
{
    string text;
    string Dummy()
    {
        return text + text;
    }
}
");
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            m_ScriptResourceStringConcat.Delete();
        }

        [Test]
        public void StringConcatIssueIsFound()
        {
            var issues = ScriptIssueTestHelper.AnalyzeAndFindScriptIssues(m_ScriptResourceStringConcat);

            Assert.AreEqual(1, issues.Count());

            Assert.True(issues.First().description.Equals("System.String.Concat"));
        }
    }
}
