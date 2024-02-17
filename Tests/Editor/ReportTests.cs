using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Tests.Common;

namespace Unity.ProjectAuditor.EditorTests
{
    class ReportTests : TestFixtureBase
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
        public void Report_NewReport_IsValid()
        {
            var report = new Report(new AnalysisParams());
            Assert.Zero(report.NumTotalIssues);
            Assert.Zero(report.GetNumIssues(IssueCategory.Code));
            Assert.Zero(report.GetNumIssues(IssueCategory.ProjectSetting));
            Assert.IsEmpty(report.FindByCategory(IssueCategory.Code));
            Assert.IsEmpty(report.FindByCategory(IssueCategory.ProjectSetting));
        }

        [Test]
        public void Report_Issue_IsAdded()
        {
            var report = new Report(new AnalysisParams());

            report.AddIssues(new[] { new ReportItem
                                     (
                                         IssueCategory.Texture,
                                         "myTexture"
                                     ) }
            );

            Assert.AreEqual(1, report.NumTotalIssues);
            Assert.AreEqual(1, report.GetNumIssues(IssueCategory.Texture));
        }
    }
}
