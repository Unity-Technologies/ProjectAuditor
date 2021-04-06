using NUnit.Framework;
using Unity.ProjectAuditor.Editor.Auditors;

namespace UnityEditor.ProjectAuditor.EditorTests
{
    public class BuildReportTests
    {
        [Test]
        public void BuildReportIsSupported()
        {
            var projectAuditor = new Unity.ProjectAuditor.Editor.ProjectAuditor();
            var auditor = projectAuditor.GetAuditor<BuildAuditor>();
            var isSupported = auditor.IsSupported();
#if UNITY_2019_4_OR_NEWER
            Assert.True(isSupported);
#else
            Assert.False(isSupported);
#endif
        }
    }
}
