using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.InstructionAnalyzers;
using Unity.ProjectAuditor.Editor.Tests.Common;

namespace Unity.ProjectAuditor.EditorTests
{
    class BoxingIssueTests : TestFixtureBase
    {
        TestAsset m_TestAssetBoxingFloat;
        TestAsset m_TestAssetBoxingGeneric;
        TestAsset m_TestAssetBoxingGenericRefType;
        TestAsset m_TestAssetBoxingInt;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            m_TestAssetBoxingInt = new TestAsset("BoxingIntTest.cs",
                "using System; class BoxingIntTest { Object Dummy() { return 666; } }");
            m_TestAssetBoxingFloat = new TestAsset("BoxingFloatTest.cs",
                "using System; class BoxingFloatTest { Object Dummy() { return 666.0f; } }");
            m_TestAssetBoxingGenericRefType = new TestAsset("BoxingGenericRefType.cs",
                "class SomeClass {}; class BoxingGenericRefType<T> where T : SomeClass { T refToGenericType; void Dummy() { if (refToGenericType == null){} } }");
            m_TestAssetBoxingGeneric = new TestAsset("BoxingGeneric.cs",
                "class BoxingGeneric<T> { T refToGenericType; void Dummy() { if (refToGenericType == null){} } }");

            AnalyzeTestAssets();
        }

        [Test]
        public void CodeAnalysis_BoxingIntValue_IsReported()
        {
            var issues = GetIssuesForAsset(m_TestAssetBoxingInt);

            Assert.AreEqual(1, issues.Count());

            var boxingInt = issues.FirstOrDefault();

            // check issue
            Assert.NotNull(boxingInt);
            Assert.AreEqual(m_TestAssetBoxingInt.FileName, boxingInt.Filename);
            Assert.AreEqual("Conversion from value type 'Int32' to ref type", boxingInt.Description);
            Assert.AreEqual("System.Object BoxingIntTest::Dummy()", boxingInt.GetContext());
            Assert.AreEqual(1, boxingInt.Line);
            Assert.AreEqual(IssueCategory.Code, boxingInt.Category);

            // check ID
            Assert.True(boxingInt.Id.IsValid());

            Assert.AreEqual(BoxingAnalyzer.PAC2000, boxingInt.Id.ToString());

            var descriptor = boxingInt.Id.GetDescriptor();
            Assert.AreEqual(Severity.Moderate, descriptor.DefaultSeverity);
            Assert.True(string.IsNullOrEmpty(descriptor.Type));
            Assert.True(string.IsNullOrEmpty(descriptor.Method));
            Assert.False(string.IsNullOrEmpty(descriptor.Title));
            Assert.AreEqual("Boxing Allocation", descriptor.Title);
        }

        [Test]
        public void CodeAnalysis_BoxingFloatValue_IsReported()
        {
            var issues = GetIssuesForAsset(m_TestAssetBoxingFloat);

            Assert.AreEqual(1, issues.Count());

            var boxingFloat = issues.FirstOrDefault();

            // check issue
            Assert.NotNull(boxingFloat);
            Assert.AreEqual(m_TestAssetBoxingFloat.FileName, boxingFloat.Filename);
            Assert.AreEqual("Conversion from value type 'float' to ref type", boxingFloat.Description);
            Assert.AreEqual("System.Object BoxingFloatTest::Dummy()", boxingFloat.GetContext());
            Assert.AreEqual(1, boxingFloat.Line);
            Assert.AreEqual(IssueCategory.Code, boxingFloat.Category);

            // check ID
            Assert.True(boxingFloat.Id.IsValid());

            Assert.AreEqual(BoxingAnalyzer.PAC2000, boxingFloat.Id.ToString());

            var descriptor = boxingFloat.Id.GetDescriptor();
            Assert.AreEqual(Severity.Moderate, descriptor.DefaultSeverity);
            Assert.True(string.IsNullOrEmpty(descriptor.Type));
            Assert.True(string.IsNullOrEmpty(descriptor.Method));
            Assert.False(string.IsNullOrEmpty(descriptor.Title));
            Assert.AreEqual("Boxing Allocation", descriptor.Title);
        }

        [Test]
        public void CodeAnalysis_BoxingGeneric_IsReported()
        {
            var issues = GetIssuesForAsset(m_TestAssetBoxingGeneric);

            Assert.AreEqual(1, issues.Count());
        }

        [Test]
        public void CodeAnalysis_BoxingGenericRefType_IsNotReported()
        {
            var issues = GetIssuesForAsset(m_TestAssetBoxingGenericRefType);

            Assert.Zero(issues.Count());
        }
    }
}
