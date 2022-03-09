using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Auditors;
using Unity.ProjectAuditor.Editor.Utils;

namespace Unity.ProjectAuditor.EditorTests
{
    class AssemblyAnalysisTests
    {
#pragma warning disable 0414
        TempAsset m_TempAsset; // this is required to generate Assembly-CSharp.dll
#pragma warning restore 0414

        [OneTimeSetUp]
        public void SetUp()
        {
            m_TempAsset = new TempAsset("MyClass.cs", @"
class MyClass
{
}
");
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            TempAsset.Cleanup();
        }

        [Test]
        public void AssemblyAnalysis_DefaultAssembly_IsReported()
        {
            var issues = Utility.Analyze(IssueCategory.Assembly, issue => issue.description.Equals(AssemblyInfo.DefaultAssemblyName));

            Assert.AreEqual(1, issues.Length);
            Assert.False(issues[0].GetCustomPropertyAsBool(AssemblyProperty.ReadOnly));
        }
    }
}
