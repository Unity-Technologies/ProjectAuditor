using System;
using System.Linq;
using NUnit.Framework;

namespace Unity.ProjectAuditor.EditorTests
{
    class StringConcatTests : TestFixtureBase
    {
        TempAsset m_TempAssetStringConcat;

        [OneTimeSetUp]
        public void SetUp()
        {
            m_TempAssetStringConcat = new TempAsset("StringConcat.cs", @"
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
            var issues = AnalyzeAndFindAssetIssues(m_TempAssetStringConcat);

            Assert.AreEqual(1, issues.Count());
            Assert.AreEqual("'System.String.Concat' usage", issues.First().description);
        }
    }
}
