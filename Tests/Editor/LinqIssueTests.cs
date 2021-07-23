using System;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.CodeAnalysis;

namespace UnityEditor.ProjectAuditor.EditorTests
{
    class LinqIssueTests
    {
        TempAsset m_TempAsset;

        [SetUp]
        public void SetUp()
        {
            m_TempAsset = new TempAsset("MyClass.cs", @"
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
            TempAsset.Cleanup();
        }

        [Test]
        public void LinqIssueIsReported()
        {
            var issues = Utility.AnalyzeAndFindAssetIssues(m_TempAsset);

            Assert.AreEqual(1, issues.Count());

            var myIssue = issues.FirstOrDefault();

            Assert.NotNull(myIssue);
            Assert.NotNull(myIssue.descriptor);

            Assert.AreEqual(Rule.Severity.Default, myIssue.descriptor.severity);
            Assert.AreEqual(101049, myIssue.descriptor.id);
            Assert.True(myIssue.descriptor.type.Equals("System.Linq"));
            Assert.True(myIssue.descriptor.method.Equals("*"));

            Assert.True(myIssue.name.Equals("Enumerable.Count"));
            Assert.True(myIssue.filename.Equals(m_TempAsset.fileName));
            Assert.True(myIssue.description.Equals("System.Linq.Enumerable.Count"), "Description: {0}", myIssue.description);
            Assert.True(
                myIssue.GetCallingMethod().Equals(
                    "System.Int32 MyClass::Dummy(System.Collections.Generic.List`1<System.Int32>)"));
            Assert.AreEqual(9, myIssue.line);
            Assert.AreEqual(IssueCategory.Code, myIssue.category);
        }
    }
}
