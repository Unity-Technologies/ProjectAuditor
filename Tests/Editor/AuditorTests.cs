using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Auditors;
using Unity.ProjectAuditor.Editor.Utils;

namespace UnityEditor.ProjectAuditor.EditorTests
{
    public class AuditorTests
    {
        [Test]
        public void CanGetBuiltinAuditorTypes()
        {
            var types = TypeInfo.GetAllTypesInheritedFromInterface<IAuditor>();

            Assert.NotNull(types.FirstOrDefault(type => type == typeof(ScriptAuditor)));
            Assert.NotNull(types.FirstOrDefault(type => type == typeof(SettingsAuditor)));
        }
    }
}
