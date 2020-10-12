using System;
using NUnit.Framework;

namespace UnityEditor.ProjectAuditor.EditorTests
{
    public class PreprocessBuildTests
    {
        [Test]
        public void ProjectAuditorCanBeInstantiated()
        {
            Activator.CreateInstance(typeof(Unity.ProjectAuditor.Editor.ProjectAuditor));
        }
    }
}
