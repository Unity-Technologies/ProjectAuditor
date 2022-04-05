using System;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.CodeAnalysis;

namespace Unity.ProjectAuditor.EditorTests
{
    class BoxingIssueTests
    {
        TempAsset m_TempAssetBoxingFloat;
        TempAsset m_TempAssetBoxingGeneric;
        TempAsset m_TempAssetBoxingGenericRefType;
        TempAsset m_TempAssetBoxingInt;

        [OneTimeSetUp]
        public void SetUp()
        {
            m_TempAssetBoxingInt = new TempAsset("BoxingIntTest.cs",
                "using System; class BoxingIntTest { Object Dummy() { return 666; } }");
            m_TempAssetBoxingFloat = new TempAsset("BoxingFloatTest.cs",
                "using System; class BoxingFloatTest { Object Dummy() { return 666.0f; } }");
            m_TempAssetBoxingGenericRefType = new TempAsset("BoxingGenericRefType.cs",
                "class SomeClass {}; class BoxingGenericRefType<T> where T : SomeClass { T refToGenericType; void Dummy() { if (refToGenericType == null){} } }");
            m_TempAssetBoxingGeneric = new TempAsset("BoxingGeneric.cs",
                "class BoxingGeneric<T> { T refToGenericType; void Dummy() { if (refToGenericType == null){} } }");
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            TempAsset.Cleanup();
        }

        [Test]
        public void CodeAnalysis_BoxingIntValue_IsReported()
        {
            var issues = Utility.AnalyzeAndFindAssetIssues(m_TempAssetBoxingInt);

            Assert.AreEqual(1, issues.Count());

            var boxingInt = issues.FirstOrDefault();

            // check issue
            Assert.NotNull(boxingInt);
            Assert.AreEqual(m_TempAssetBoxingInt.fileName, boxingInt.filename);
            Assert.AreEqual("Conversion from value type 'Int32' to ref type", boxingInt.description);
            Assert.AreEqual("System.Object BoxingIntTest::Dummy()", boxingInt.GetCallingMethod());
            Assert.AreEqual(1, boxingInt.line);
            Assert.AreEqual(IssueCategory.Code, boxingInt.category);

            // check descriptor
            Assert.NotNull(boxingInt.descriptor);
            Assert.AreEqual(Rule.Severity.Default, boxingInt.descriptor.severity);
            Assert.AreEqual(102000, boxingInt.descriptor.id);
            Assert.True(string.IsNullOrEmpty(boxingInt.descriptor.type));
            Assert.True(string.IsNullOrEmpty(boxingInt.descriptor.method));
            Assert.False(string.IsNullOrEmpty(boxingInt.descriptor.description));
            Assert.AreEqual("Boxing Allocation", boxingInt.descriptor.description);
        }

        [Test]
        public void CodeAnalysis_BoxingFloatValue_IsReported()
        {
            var issues = Utility.AnalyzeAndFindAssetIssues(m_TempAssetBoxingFloat);

            Assert.AreEqual(1, issues.Count());

            var boxingFloat = issues.FirstOrDefault();

            // check issue
            Assert.NotNull(boxingFloat);
            Assert.AreEqual(m_TempAssetBoxingFloat.fileName, boxingFloat.filename);
            Assert.AreEqual("Conversion from value type 'float' to ref type", boxingFloat.description);
            Assert.AreEqual("System.Object BoxingFloatTest::Dummy()", boxingFloat.GetCallingMethod());
            Assert.AreEqual(1, boxingFloat.line);
            Assert.AreEqual(IssueCategory.Code, boxingFloat.category);

            // check descriptor
            Assert.NotNull(boxingFloat.descriptor);
            Assert.AreEqual(Rule.Severity.Default, boxingFloat.descriptor.severity);
            Assert.AreEqual(102000, boxingFloat.descriptor.id);
            Assert.True(string.IsNullOrEmpty(boxingFloat.descriptor.type));
            Assert.True(string.IsNullOrEmpty(boxingFloat.descriptor.method));
            Assert.False(string.IsNullOrEmpty(boxingFloat.descriptor.description));
            Assert.AreEqual("Boxing Allocation", boxingFloat.descriptor.description);
        }

        [Test]
        public void CodeAnalysis_BoxingGeneric_IsReported()
        {
            var issues = Utility.AnalyzeAndFindAssetIssues(m_TempAssetBoxingGeneric);

            Assert.AreEqual(1, issues.Count());
        }

        [Test]
        public void CodeAnalysis_BoxingGenericRefType_IsNotReported()
        {
            var issues = Utility.AnalyzeAndFindAssetIssues(m_TempAssetBoxingGenericRefType);

            Assert.Zero(issues.Count());
        }
    }
}
