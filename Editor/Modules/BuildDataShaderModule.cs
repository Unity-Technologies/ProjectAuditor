using System.Collections.Generic;
using System.Linq;
using Unity.ProjectAuditor.Editor.Build;
using Unity.ProjectAuditor.Editor.BuildData;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Interfaces;
using UnityEditor;
using Unity.ProjectAuditor.Editor.UnityFileSystemApi;
using Unity.ProjectAuditor.Editor.BuildData.SerializedObjects;

namespace Unity.ProjectAuditor.Editor.Modules
{
    enum BuildDataShaderProperty
    {
        AssetBundle,
        DecompressedSize,
        SubShaders,
        SubPrograms,
        Keywords,
        Num,
    }

    class BuildDataShaderModule : ModuleWithAnalyzers<IBuildDataShaderModuleAnalyzer>
    {
        static readonly IssueLayout k_ShaderLayout = new IssueLayout
        {
            category = IssueCategory.BuildDataShader,
            properties = new[]
            {
                new PropertyDefinition { type = PropertyType.Description, format = PropertyFormat.String, name = "Name", longName = "Shader Name" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(BuildDataShaderProperty.AssetBundle), format = PropertyFormat.String, name = "Asset Bundle", longName = "Asset Bundle Name" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(BuildDataShaderProperty.DecompressedSize), format = PropertyFormat.Bytes, name = "Decompressed Size", longName = "Decompressed Size" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(BuildDataShaderProperty.SubShaders), format = PropertyFormat.Integer, name = "Sub Shaders", longName = "Number Of Sub Shaders" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(BuildDataShaderProperty.SubPrograms), format = PropertyFormat.Integer, name = "Sub Programs", longName = "Number Of Sub Programs In Sub Shaders" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(BuildDataShaderProperty.Keywords), format = PropertyFormat.String, name = "Keywords", longName = "Keywords" },
            }
        };

        public override string Name => "Shaders";

        public override bool IsEnabledByDefault => false;

        public override IReadOnlyCollection<IssueLayout> SupportedLayouts => new IssueLayout[]
        {
            k_ShaderLayout
        };

        public override void Audit(AnalysisParams projectAuditorParams, IProgress progress = null)
        {
            var analyzers = GetPlatformAnalyzers(projectAuditorParams.Platform);

            if (projectAuditorParams.BuildAnalyzer != null)
            {
                var shaders = projectAuditorParams.BuildAnalyzer.GetSerializedObjects<Shader>();

                progress?.Start("Parsing Shaders from Build Data", "Search in Progress...", shaders.Count());

                foreach (var shader in shaders)
                {
                    foreach (var analyzer in analyzers)
                    {
                        var context = new BuildDataShaderAnalyzerContext
                        {
                            Shader = shader,
                            Params = projectAuditorParams
                        };

                        projectAuditorParams.OnIncomingIssues(analyzer.Analyze(context));
                    }

                    progress?.Advance();
                }
            }

            progress?.Clear();

            projectAuditorParams.OnModuleCompleted?.Invoke();
        }
    }
}
