using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Tests.Common;

namespace Unity.ProjectAuditor.EditorTests
{
    class ProjectReportTests : TestFixtureBase
    {
        readonly Descriptor m_Descriptor = new Descriptor
            (
            "TDD2001",
            "test",
            Areas.CPU,
            "this is not actually a problem",
            "do nothing"
            );

#pragma warning disable 0414
        TestAsset m_TestAsset;
#pragma warning restore 0414

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            DescriptorLibrary.RegisterDescriptor(m_Descriptor.Id, m_Descriptor);

            m_TestAsset = new TestAsset("MyClass.cs", @"
using UnityEngine;
class MyClass : MonoBehaviour
{
    void Update()
    {
        Debug.Log(Camera.allCameras.Length);
    }
}
");
        }

        [Test]
        public void ProjectReport_NewReport_IsValid()
        {
            var projectReport = new ProjectReport(new AnalysisParams());
            Assert.Zero(projectReport.NumTotalIssues);
            Assert.Zero(projectReport.GetNumIssues(IssueCategory.Code));
            Assert.Zero(projectReport.GetNumIssues(IssueCategory.ProjectSetting));
            Assert.IsEmpty(projectReport.FindByCategory(IssueCategory.Code));
            Assert.IsEmpty(projectReport.FindByCategory(IssueCategory.ProjectSetting));
        }

        [Test]
        public void ProjectReport_Issue_IsAdded()
        {
            var projectReport = new ProjectReport(new AnalysisParams());

            projectReport.AddIssues(new[] { new ReportItem
                                            (
                                                IssueCategory.Texture,
                                                "myTexture"
                                            ) }
            );

            Assert.AreEqual(1, projectReport.NumTotalIssues);
            Assert.AreEqual(1, projectReport.GetNumIssues(IssueCategory.Texture));
        }
    }
}
