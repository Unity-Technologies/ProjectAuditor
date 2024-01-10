using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Modules;
using Unity.ProjectAuditor.Editor.Tests.Common;
using UnityEngine;

namespace Unity.ProjectAuditor.EditorTests
{
    class MetadataTests : TestFixtureBase
    {
        [Test]
        public void Metadata_IsReported()
        {
            var report = m_ProjectAuditor.Audit(new AnalysisParams
            {
                Categories = new[]
                {
                    IssueCategory.ProjectSetting,
                }
            });

            Assert.IsNotNull(report.SessionInfo);
            Assert.AreEqual(Application.unityVersion, report.SessionInfo.UnityVersion);
        }
    }
}
