using System.Collections.Generic;
using System.Linq;
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
                int passCount = 0;
                foreach (var pass in subShader.Passes)
                {
                    foreach (var program in pass.Programs)
                    {
                        var subPrograms = program.Value;
                        subProgramCount += subPrograms.Count;

                        foreach (var subprog in subPrograms)
                        {
                            var keywordsAsStrings = subprog.Keywords.Select(i => context.Shader.Keywords[i]);
                            var keywordString = string.Join(", ", keywordsAsStrings);

                            yield return context.CreateInsight(IssueCategory.BuildDataShaderVariant, context.Shader.Name)
                                .WithCustomProperties(
                                    new object[((int)BuildDataShaderVariantProperty.Num)]
                                    {
                                        false,
                                        subprog.Api.ToString(),
                                        subprog.HwTier.ToString(),
                                        program.Key, // Stage
                                        string.IsNullOrEmpty(pass.Name) ? "Pass " + passCount.ToString() : pass.Name,
                                        keywordString,
                                        context.Shader.BuildFile.DisplayName,
                                    });
                        }
                    }

                    passCount++;
                }
            }

            var dependencyNode = new SimpleDependencyNode($"Keywords ({context.Shader.Keywords.Count})");

            foreach (var keyword in context.Shader.Keywords)
            {
                var childDependencyNode = new SimpleDependencyNode(keyword);
                dependencyNode.AddChild(childDependencyNode);
            }

            yield return context.CreateInsight(IssueCategory.BuildDataShader, context.Shader.Name)
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
