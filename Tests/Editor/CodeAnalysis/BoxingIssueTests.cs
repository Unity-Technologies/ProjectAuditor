using System;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.CodeAnalysis;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using Unity.ProjectAuditor.Editor.InstructionAnalyzers;
using Unity.ProjectAuditor.Editor.Tests.Common;
using Unity.ProjectAuditor.Editor.Utils;

namespace Unity.ProjectAuditor.EditorTests
{
    class BoxingIssueTests : TestFixtureBase
    {
        TestAsset m_TestAssetBoxingFloat;
        TestAsset m_TestAssetBoxingGeneric;
        TestAsset m_TestAssetBoxingGenericRefType;
        TestAsset m_TestAssetBoxingInt;

        [OneTimeSetUp]
        public void SetUp()
        {
            m_TestAssetBoxingInt = new TestAsset("BoxingIntTest.cs",
                "using System; class BoxingIntTest { Object Dummy() { return 666; } }");
            m_TestAssetBoxingFloat = new TestAsset("BoxingFloatTest.cs",
                "using System; class BoxingFloatTest { Object Dummy() { return 666.0f; } }");
            m_TestAssetBoxingGenericRefType = new TestAsset("BoxingGenericRefType.cs",
                "class SomeClass {}; class BoxingGenericRefType<T> where T : SomeClass { T refToGenericType; void Dummy() { if (refToGenericType == null){} } }");
            m_TestAssetBoxingGeneric = new TestAsset("BoxingGeneric.cs",
                "class BoxingGeneric<T> { T refToGenericType; void Dummy() { if (refToGenericType == null){} } }");
        }

        [Test]
        public void CodeAnalysis_BoxingIntValue_IsReported()
        {
            var issues = AnalyzeAndFindAssetIssues(m_TestAssetBoxingInt);

            Assert.AreEqual(1, issues.Count());

            var boxingInt = issues.FirstOrDefault();

            // check issue
            Assert.NotNull(boxingInt);
            Assert.AreEqual(m_TestAssetBoxingInt.fileName, boxingInt.filename);
            Assert.AreEqual("Conversion from value type 'Int32' to ref type", boxingInt.description);
            Assert.AreEqual("System.Object BoxingIntTest::Dummy()", boxingInt.GetContext());
            Assert.AreEqual(1, boxingInt.line);
            Assert.AreEqual(IssueCategory.Code, boxingInt.category);

            // check ID
            Assert.True(boxingInt.id.IsValid());

            Assert.AreEqual(BoxingAnalyzer.PAC2000, boxingInt.id);

            var descriptor = boxingInt.id.GetDescriptor();
            Assert.AreEqual(Severity.Moderate, descriptor.defaultSeverity);
            Assert.True(string.IsNullOrEmpty(descriptor.type));
            Assert.True(string.IsNullOrEmpty(descriptor.method));
            Assert.False(string.IsNullOrEmpty(descriptor.title));
            Assert.AreEqual("Boxing Allocation", descriptor.title);
        }

        [Test]
        public void CodeAnalysis_BoxingFloatValue_IsReported()
        {
            var issues = AnalyzeAndFindAssetIssues(m_TestAssetBoxingFloat);

            Assert.AreEqual(1, issues.Count());

            var boxingFloat = issues.FirstOrDefault();

            // check issue
            Assert.NotNull(boxingFloat);
            Assert.AreEqual(m_TestAssetBoxingFloat.fileName, boxingFloat.filename);
            Assert.AreEqual("Conversion from value type 'float' to ref type", boxingFloat.description);
            Assert.AreEqual("System.Object BoxingFloatTest::Dummy()", boxingFloat.GetContext());
            Assert.AreEqual(1, boxingFloat.line);
            Assert.AreEqual(IssueCategory.Code, boxingFloat.category);

            // check ID
            Assert.True(boxingFloat.id.IsValid());

            Assert.AreEqual(BoxingAnalyzer.PAC2000, boxingFloat.id);

            var descriptor = boxingFloat.id.GetDescriptor();
            Assert.AreEqual(Severity.Moderate, descriptor.defaultSeverity);
            Assert.True(string.IsNullOrEmpty(descriptor.type));
            Assert.True(string.IsNullOrEmpty(descriptor.method));
            Assert.False(string.IsNullOrEmpty(descriptor.title));
            Assert.AreEqual("Boxing Allocation", descriptor.title);
        }

        [Test]
        public void CodeAnalysis_BoxingGeneric_IsReported()
        {
            var issues = AnalyzeAndFindAssetIssues(m_TestAssetBoxingGeneric);

            Assert.AreEqual(1, issues.Count());
        }

        [Test]
        public void CodeAnalysis_BoxingGenericRefType_IsNotReported()
        {
            var issues = AnalyzeAndFindAssetIssues(m_TestAssetBoxingGenericRefType);

            Assert.Zero(issues.Count());
        }
    }
}
