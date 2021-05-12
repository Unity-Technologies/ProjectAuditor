using System;
using System.Linq;
using NUnit.Framework;

namespace UnityEditor.ProjectAuditor.EditorTests
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
        public void StringConcatIssueIsFound()
        {
            var issues = Utility.AnalyzeAndFindAssetIssues(m_TempAssetStringConcat);

            Assert.AreEqual(1, issues.Count());

            Assert.True(issues.First().description.Equals("System.String.Concat"));
        }
    }
}
