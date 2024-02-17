using NUnit.Framework;
using System;
using System.Linq;
using System.Reflection;

namespace Unity.ProjectAuditor.EditorTests
{
    class NamespaceTests
    {
        [Test]
        public void Namespaces_Are_Prefixed()
        {
            var assemblyPrefix = "Unity.ProjectAuditor.";
            var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            var assemblies = loadedAssemblies.Where(a => a.FullName.StartsWith(assemblyPrefix));
            var types = assemblies.SelectMany(a => a.GetTypes());

            Assert.Positive(assemblies.Count(), $"ProjectAuditor assemblies are not found.");
            Assert.Positive(types.Count(), $"ProjectAuditor types are not found.");

            foreach (var t in types)
            {
                if (t.FullName.StartsWith("<PrivateImplementationDetails>"))
                    continue;
                if (t.FullName.StartsWith("UnitySourceGeneratedAssembly"))
                    continue;
                Assert.IsTrue(t.Namespace != null && t.Namespace.StartsWith(assemblyPrefix), $"Type's namespace {t.FullName} does not start with {assemblyPrefix}");
            }
        }
    }
}
