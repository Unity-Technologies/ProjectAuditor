using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;

namespace Unity.ProjectAuditor.EditorTests
{
    class MeshTests : TestFixtureBase
    {
        const string k_SmallMeshName = "SmallTestMesh";
        const string k_LargeMeshName = "LargeTestMesh";

        TempAsset m_TempSmallMeshAsset;
        TempAsset m_TempLargeMeshAsset;

        [OneTimeSetUp]
        public void SetUp()
        {
            var smallMesh = MeshGeneratorUtil.CreateTestMesh(k_SmallMeshName, 100);
            m_TempSmallMeshAsset = TempAsset.Save(smallMesh, k_SmallMeshName + ".mesh");

            var largeMesh = MeshGeneratorUtil.CreateTestMesh(k_LargeMeshName, 100000, true);
            m_TempLargeMeshAsset = TempAsset.Save(largeMesh, k_LargeMeshName + ".mesh");
        }

        [OneTimeTearDown]
        public void TearDown()
        {
        }

        [Test]
#if !UNITY_2019_3_OR_NEWER
        [Ignore("This requires the new Mesh API")]
#endif
        public void Mesh_Using32bitIndexFormat_IsReported()
        {
            var foundIssues = AnalyzeAndFindAssetIssues(m_TempSmallMeshAsset, IssueCategory.AssetDiagnostic);

            Assert.IsNotEmpty(foundIssues);
            Assert.IsTrue(foundIssues.Any(issue => issue.descriptor.id == "PAM0001"), "Small mesh should be reported");
        }

        [Test]
        public void Mesh_ReadWrite_IsReported()
        {
            var foundIssues = AnalyzeAndFindAssetIssues(m_TempSmallMeshAsset, IssueCategory.AssetDiagnostic);

            Assert.IsNotEmpty(foundIssues);
            Assert.IsTrue(foundIssues.Any(issue => issue.descriptor.id == "PAM0000"), "Read/Write mesh should be reported");
        }

        [Test]
        public void Mesh_ReadWrite_IsNotReported()
        {
            var foundIssues = AnalyzeAndFindAssetIssues(m_TempLargeMeshAsset, IssueCategory.AssetDiagnostic);

            Assert.IsFalse(foundIssues.Any(issue => issue.descriptor.id == "PAM0000"), "Read/Write mesh should no be reported");
        }
    }
}
