using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Modules;
using Unity.ProjectAuditor.Editor.TestUtils;

namespace Unity.ProjectAuditor.EditorTests
{
    class MeshTests : TestFixtureBase
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
#if !UNITY_2019_3_OR_NEWER
        [Ignore("This requires the new Mesh API")]
#endif
        public void Mesh_Using32bitIndexFormat_IsReported()
        {
            var foundIssues = AnalyzeAndFindAssetIssues(m_TestSmallMeshAsset, IssueCategory.AssetDiagnostic);

            Assert.IsNotEmpty(foundIssues);
            Assert.IsTrue(foundIssues.Any(issue => issue.descriptor.id == MeshAnalyzer.PAM0001), "Small mesh should be reported");
        }

        [Test]
        public void Mesh_ReadWrite_IsReported()
        {
            var foundIssues = AnalyzeAndFindAssetIssues(m_TestSmallMeshAsset, IssueCategory.AssetDiagnostic);

            Assert.IsNotEmpty(foundIssues);
            Assert.IsTrue(foundIssues.Any(issue => issue.descriptor.id == MeshAnalyzer.PAM0000), "Read/Write mesh should be reported");
        }

        [Test]
        public void Mesh_ReadWrite_IsNotReported()
        {
            var foundIssues = AnalyzeAndFindAssetIssues(m_TestLargeMeshAsset, IssueCategory.AssetDiagnostic);

            Assert.IsFalse(foundIssues.Any(issue => issue.descriptor.id == MeshAnalyzer.PAM0000), "Read/Write mesh should no be reported");
        }
    }
}
