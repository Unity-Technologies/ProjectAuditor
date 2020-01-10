using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor.Utils;

namespace UnityEditor.ProjectAuditor.EditorTests
{
    public class AssemblyHelperTests
    {
        [Test]
        public void DefaultAssemblyPathIsFound()
        {
            AssemblyHelper.CompileAssemblies();
            var paths = AssemblyHelper.GetCompiledAssemblyPaths();

            Assert.Positive(paths.Count());
            Assert.True(paths.First().Contains("Assembly-CSharp.dll"));
        }
        
        [Test]
        public void UnityEngineAssemblyPathIsFound()
        {
            var paths = AssemblyHelper.GetPrecompiledEngineAssemblyPaths();

            Assert.Positive(paths.Count());
            Assert.NotNull(paths.Where(path => path.Contains("UnityEngine.CoreModule.dll")).FirstOrDefault());
        }
    }
}