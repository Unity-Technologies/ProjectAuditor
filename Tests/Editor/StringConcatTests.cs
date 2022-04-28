using System;
using System.Linq;
using NUnit.Framework;

namespace Unity.ProjectAuditor.EditorTests
{
    class StringConcatTests
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

        [OneTimeTearDown]
        public void TearDown()
        {
            TempAsset.Cleanup();
        }

        [Test]
        public void CodeAnalysis_StringConcat_IsReported()
        {
            var issues = Utility.AnalyzeAndFindAssetIssues(m_TempAssetStringConcat);

            Assert.AreEqual(1, issues.Count());
            Assert.AreEqual("'System.String.Concat' usage", issues.First().description);
        }
    }
}
