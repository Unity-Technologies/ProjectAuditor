using System;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.CodeAnalysis;
using Unity.ProjectAuditor.Editor.Utils;

namespace Unity.ProjectAuditor.EditorTests
{
    class LinqIssueTests : TestFixtureBase
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

        [Test]
        public void CodeAnalysis_LinqIssue_IsReported()
        {
            var issues = AnalyzeAndFindAssetIssues(m_TempAsset);

            Assert.AreEqual(1, issues.Count());

            var myIssue = issues.FirstOrDefault();

            Assert.NotNull(myIssue);
            Assert.NotNull(myIssue.descriptor);

            Assert.AreEqual(Rule.Severity.Default, myIssue.descriptor.severity);
            Assert.AreEqual("PAC1000", myIssue.descriptor.id);
            Assert.AreEqual("System.Linq", myIssue.descriptor.type);
            Assert.AreEqual("*", myIssue.descriptor.method);

            Assert.AreEqual(m_TempAsset.fileName, myIssue.filename);
            Assert.AreEqual("'System.Linq.Enumerable.Count' usage", myIssue.description, "Description: {0}", myIssue.description);
            Assert.AreEqual("System.Int32 MyClass::Dummy(System.Collections.Generic.List`1<System.Int32>)", myIssue.GetContext());
            Assert.AreEqual(9, myIssue.line);
            Assert.AreEqual(IssueCategory.Code, myIssue.category);
        }
    }
}
