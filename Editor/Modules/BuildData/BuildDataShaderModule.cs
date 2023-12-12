using System.Collections.Generic;
using System.Linq;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Interfaces;
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

    enum BuildDataShaderVariantProperty
    {
        Compiled,
        GraphicsAPI,
        Tier,
        Stage,
        PassType,
        PassName,
        Keywords,
        PlatformKeywords,
        Requirements,
        AssetBundle,
        Num,
    }

    class BuildDataShaderModule : ModuleWithAnalyzers<IBuildDataShaderModuleAnalyzer>
    {
        static readonly IssueLayout k_ShaderLayout = new IssueLayout
        {
            Category = IssueCategory.BuildDataShader,
            Properties = new[]
            {
                new PropertyDefinition { Type = PropertyType.Description, Format = PropertyFormat.String, Name = "Shader Name", LongName = "Shader Name" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(BuildDataShaderProperty.AssetBundle), Format = PropertyFormat.String, Name = "File", LongName = "File Name" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(BuildDataShaderProperty.DecompressedSize), Format = PropertyFormat.Bytes, Name = "Decompressed Size", LongName = "Decompressed Size" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(BuildDataShaderProperty.SubShaders), Format = PropertyFormat.Integer, Name = "Sub Shaders", LongName = "Number Of Sub Shaders" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(BuildDataShaderProperty.SubPrograms), Format = PropertyFormat.Integer, Name = "Sub Programs", LongName = "Number Of Sub Programs In Sub Shaders" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(BuildDataShaderProperty.Keywords), Format = PropertyFormat.String, Name = "Keywords", LongName = "Keywords" },
            }
        };

        static readonly IssueLayout k_ShaderVariantLayout = new IssueLayout
        {
            Category = IssueCategory.BuildDataShaderVariant,
            Properties = new[]
            {
                new PropertyDefinition { Type = PropertyType.Description, Format = PropertyFormat.String, Name = "Shader Name", LongName = "Shader Name" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(BuildDataShaderVariantProperty.Compiled), Format = PropertyFormat.Bool, Name = "Compiled", LongName = "Compiled During Runtime" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(BuildDataShaderVariantProperty.GraphicsAPI), Format = PropertyFormat.String, Name = "Graphics API", LongName = "Graphics API" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(BuildDataShaderVariantProperty.Tier), Format = PropertyFormat.String, Name = "Tier", LongName = "Tier" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(BuildDataShaderVariantProperty.Stage), Format = PropertyFormat.String, Name = "Stage", LongName = "Stage" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(BuildDataShaderVariantProperty.PassType), Format = PropertyFormat.String, Name = "Pass Type", LongName = "Pass Type" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(BuildDataShaderVariantProperty.PassName), Format = PropertyFormat.String, Name = "Pass Name", LongName = "Pass Name" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(BuildDataShaderVariantProperty.Keywords), Format = PropertyFormat.String, Name = "Keywords", LongName = "Keywords" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(BuildDataShaderVariantProperty.PlatformKeywords), Format = PropertyFormat.String, Name = "Platform Keywords", LongName = "Platform Keywords" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(BuildDataShaderVariantProperty.PlatformKeywords), Format = PropertyFormat.String, Name = "Requirements", LongName = "Requirements" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(BuildDataShaderVariantProperty.AssetBundle), Format = PropertyFormat.String, Name = "File", LongName = "File Name" },
            }
        };


        public override string Name => "Shaders";

        public override IReadOnlyCollection<IssueLayout> SupportedLayouts => new IssueLayout[]
        {
            k_ShaderLayout,
            k_ShaderVariantLayout
        };

        public override AnalysisResult Audit(AnalysisParams projectAuditorParams, IProgress progress = null)
        {
            var analyzers = GetPlatformAnalyzers(projectAuditorParams.Platform);

            if (projectAuditorParams.BuildObjects != null)
            {
                var shaders = projectAuditorParams.BuildObjects.GetObjects<Shader>();

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

            return AnalysisResult.Success;
        }
    }
}
