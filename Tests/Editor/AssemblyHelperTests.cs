using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor.Utils;

namespace UnityEditor.ProjectAuditor.EditorTests
{
    public class AssemblyHelperTests
    {
        private ScriptResource m_ScriptResource;

        [OneTimeSetUp]
        public void SetUp()
        {
            // this is required so the default assembly is generated
            m_ScriptResource = new ScriptResource("MyClass.cs", "class MyClass { }");
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            m_ScriptResource.Delete();
        }
        
        [Test]
        public void DefaultAssemblyPathIsFound()
        {
            AssemblyHelper.CompileAssemblies();
            var paths = AssemblyHelper.GetCompiledAssemblyPaths();

            Assert.Positive(paths.Count());
            Assert.NotNull(paths.FirstOrDefault(path => path.Contains("Assembly-CSharp.dll")));
        }
        
        [Test]
        public void UnityEngineAssemblyPathIsFound()
        {
            var paths = AssemblyHelper.GetPrecompiledEngineAssemblyPaths();

            Assert.Positive(paths.Count());
            Assert.NotNull(paths.FirstOrDefault(path => path.Contains("UnityEngine.CoreModule.dll")));
        }
    }
}