using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Modules;
using Unity.ProjectAuditor.Editor.Tests.Common;

namespace Unity.ProjectAuditor.EditorTests
{
    internal class MeshTests : TestFixtureBase
    {
        const string k_SmallMeshName = "SmallTestMesh";
        const string k_LargeMeshName = "LargeTestMesh";

        TestAsset m_TestSmallMeshAsset;
        TestAsset m_TestLargeMeshAsset;

        [OneTimeSetUp]
        public void SetUp()
        {
            var smallMesh = MeshGeneratorUtil.CreateTestMesh(k_SmallMeshName, 100);
            m_TestSmallMeshAsset = TestAsset.Save(smallMesh, k_SmallMeshName + ".mesh");

            var largeMesh = MeshGeneratorUtil.CreateTestMesh(k_LargeMeshName, 100000, true);
            m_TestLargeMeshAsset = TestAsset.Save(largeMesh, k_LargeMeshName + ".mesh");
        }

        [OneTimeTearDown]
        public void TearDown()
        {
        }

        [Test]
        public void Mesh_Using32bitIndexFormat_IsReported()
        {
            var foundIssues = AnalyzeAndFindAssetIssues(m_TestSmallMeshAsset, IssueCategory.AssetIssue);

            Assert.IsNotEmpty(foundIssues);
            Assert.IsTrue(foundIssues.Any(issue => issue.Id == MeshAnalyzer.PAA1001), "Small mesh should be reported");
        }

        [Test]
        public void Mesh_ReadWrite_IsReported()
        {
            var foundIssues = AnalyzeAndFindAssetIssues(m_TestSmallMeshAsset, IssueCategory.AssetIssue);

            Assert.IsNotEmpty(foundIssues);
            Assert.IsTrue(foundIssues.Any(issue => issue.Id == MeshAnalyzer.PAA1000), "Read/Write mesh should be reported");
        }

        [Test]
        public void Mesh_ReadWrite_IsNotReported()
        {
            var foundIssues = AnalyzeAndFindAssetIssues(m_TestLargeMeshAsset, IssueCategory.AssetIssue);

            Assert.IsFalse(foundIssues.Any(issue => issue.Id == MeshAnalyzer.PAA1000), "Read/Write mesh should no be reported");
        }
    }
}
