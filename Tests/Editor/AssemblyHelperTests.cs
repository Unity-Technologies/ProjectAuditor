using System.IO;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor.Compilation;

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
            using (var compilationHelper = new AssemblyCompilationHelper())
            {
                var assemblyInfos = compilationHelper.Compile();

                Assert.Positive(assemblyInfos.Count());
                Assert.NotNull(assemblyInfos.FirstOrDefault(info => info.name.Contains(AssemblyHelper.DefaultAssemblyFileName)));
            }
        }

        [Test]
        public void UnityEngineAssemblyPathIsFound()
        {
            var paths = AssemblyHelper.GetPrecompiledEngineAssemblyPaths();

            Assert.Positive(paths.Count());
            Assert.NotNull(paths.FirstOrDefault(path => path.Contains("UnityEngine.CoreModule.dll")));
        }

#if UNITY_2018_1_OR_NEWER
        [Test]
        public void DefaultAssemblyInfoIsCorrect()
        {
            var assembly = CompilationPipeline.GetAssemblies(AssembliesType.Player).FirstOrDefault(a => a.name.Equals(Path.GetFileNameWithoutExtension(AssemblyHelper.DefaultAssemblyFileName)));
            var assemblyInfo = AssemblyHelper.GetAssemblyInfoFromAssemblyPath(assembly.outputPath);

            Assert.IsTrue(assemblyInfo.path.Equals("Library/ScriptAssemblies/Assembly-CSharp.dll"));
            Assert.IsNull(assemblyInfo.asmDefPath);
            Assert.IsFalse(assemblyInfo.readOnly);
        }

        [Test]
        public void PackageAssemblyInfoIsCorrect()
        {
            var assembly = CompilationPipeline.GetAssemblies(AssembliesType.Editor).FirstOrDefault(a => a.name.Equals("Unity.ProjectAuditor.Editor"));
            var assemblyInfo = AssemblyHelper.GetAssemblyInfoFromAssemblyPath(assembly.outputPath);

            Assert.IsTrue(assemblyInfo.path.Equals("Library/ScriptAssemblies/Unity.ProjectAuditor.Editor.dll"));
            Assert.IsTrue(assemblyInfo.asmDefPath.Equals("Packages/com.unity.project-auditor/Editor/Unity.ProjectAuditor.Editor.asmdef"));
            Assert.IsTrue(assemblyInfo.relativePath.Equals("Packages/com.unity.project-auditor"));
        }
#endif
    }
}
