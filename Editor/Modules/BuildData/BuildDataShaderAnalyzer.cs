using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Interfaces;

namespace Unity.ProjectAuditor.Editor.Modules
{
    class BuildDataShaderAnalyzer : IBuildDataShaderModuleAnalyzer
    {
        public void Initialize(Module module)
        {
        }

        public IEnumerable<ProjectIssue> Analyze(BuildDataShaderAnalyzerContext context)
        {
            int subProgramCount = 0;
            foreach (var subShader in context.Shader.SubShaders)
            {
                foreach (var pass in subShader.Passes)
                {
                    foreach (var program in pass.Programs)
                    {
                        var subPrograms = program.Value;
                        subProgramCount += subPrograms.Count;
                    }
                }
            }

            var dependencyNode = new SimpleDependencyNode($"Keywords ({context.Shader.Keywords.Count})");

            foreach (var keyword in context.Shader.Keywords)
            {
                var childDependencyNode = new SimpleDependencyNode(keyword);
                dependencyNode.AddChild(childDependencyNode);
            }

            yield return context.CreateWithoutDiagnostic(IssueCategory.BuildDataShader, context.Shader.Name)
                .WithCustomProperties(
                    new object[((int)BuildDataShaderProperty.Num)]
                    {
                        context.Shader.BuildFile.DisplayName,
                        context.Shader.DecompressedSize,
                        context.Shader.SubShaders.Count,
                        subProgramCount,
                        string.Join(", ", context.Shader.Keywords)
                    })
                .WithDependencies(dependencyNode);
        }
    }
}
