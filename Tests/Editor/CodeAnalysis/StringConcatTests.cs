using System;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor.TestUtils;

namespace Unity.ProjectAuditor.EditorTests
{
    class StringConcatTests : TestFixtureBase
    {
        TestAsset m_TestAssetStringConcat;

        [OneTimeSetUp]
        public void SetUp()
        {
            m_TestAssetStringConcat = new TestAsset("StringConcat.cs", @"
class StringConcat
{
    string text = ""This is a test"";
    string Dummy()
    {
        return text + text;
    }
}
");
        }

        [Test]
        public void CodeAnalysis_StringConcat_IsReported()
        {
            var issues = AnalyzeAndFindAssetIssues(m_TestAssetStringConcat);

            Assert.AreEqual(1, issues.Count());
            Assert.AreEqual("'System.String.Concat' usage", issues.First().description);
        }
    }
}
