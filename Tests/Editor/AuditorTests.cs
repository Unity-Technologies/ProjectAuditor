using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Auditors;
using Unity.ProjectAuditor.Editor.Utils;

namespace UnityEditor.ProjectAuditor.EditorTests
{
    class AuditorTests
    {
        [Test]
        public void CanGetBuiltinAuditorTypes()
        {
            var types = TypeCache.GetTypesDerivedFrom(typeof(ProjectAuditorModule));

            Assert.NotNull(types.FirstOrDefault(type => type == typeof(CodeModule)));
            Assert.NotNull(types.FirstOrDefault(type => type == typeof(SettingsModule)));
        }
    }
}
