using System;
using NUnit.Framework;

namespace Unity.ProjectAuditor.EditorTests
{
    class ProjectAuditorTests
    {
        [Test]
        public void ProjectAuditor_IsInstantiated()
        {
            Activator.CreateInstance(typeof(Unity.ProjectAuditor.Editor.ProjectAuditor));
        }
    }
}
