using System;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.CodeAnalysis;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using Unity.ProjectAuditor.Editor.Tests.Common;
using Unity.ProjectAuditor.Editor.Utils;

namespace Unity.ProjectAuditor.EditorTests
{
    class LinqIssueTests : TestFixtureBase
    {
        TestAsset m_TestAsset;

        [SetUp]
        public void SetUp()
        {
            m_TestAsset = new TestAsset("MyClass.cs", @"
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
            var issues = AnalyzeAndFindAssetIssues(m_TestAsset);

            Assert.AreEqual(1, issues.Count());

            var myIssue = issues.FirstOrDefault();

            Assert.NotNull(myIssue);
            var descriptor = DescriptorLibrary.GetDescriptor(myIssue.id);

            Assert.AreEqual(Severity.Moderate, descriptor.defaultSeverity);
            Assert.AreEqual("PAC1000", myIssue.id);
            Assert.AreEqual("System.Linq", descriptor.type);
            Assert.AreEqual("*", descriptor.method);

            Assert.AreEqual(m_TestAsset.fileName, myIssue.filename);
            Assert.AreEqual("'System.Linq.Enumerable.Count' usage", myIssue.description, "Description: {0}", myIssue.description);
            Assert.AreEqual("System.Int32 MyClass::Dummy(System.Collections.Generic.List`1<System.Int32>)", myIssue.GetContext());
            Assert.AreEqual(9, myIssue.line);
            Assert.AreEqual(IssueCategory.Code, myIssue.category);
        }
    }
}
