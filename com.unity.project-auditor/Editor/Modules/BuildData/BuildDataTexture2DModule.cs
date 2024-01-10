using System.Collections.Generic;
using System.Linq;
using Unity.ProjectAuditor.Editor.BuildData.SerializedObjects;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Interfaces;

namespace Unity.ProjectAuditor.Editor.Modules
{
    enum BuildDataTextureProperty
    {
        Size,
        Width,
        Height,
        Format,
        MipCount,
        RwEnabled,
        AssetBundle,
        Num,
    }

    class BuildDataTexture2DModule : ModuleWithAnalyzers<IBuildDataTexture2DModuleAnalyzer>
    {
        static readonly IssueLayout k_TextureLayout = new IssueLayout
        {
            Category = IssueCategory.BuildDataTexture2D,
            Properties = new[]
            {
                new PropertyDefinition { Type = PropertyType.Description, Format = PropertyFormat.String, Name = "Name", LongName = "Texture Name" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(BuildDataTextureProperty.Size), Format = PropertyFormat.Bytes, Name = "Size", LongName = "Size" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(BuildDataTextureProperty.Width), Format = PropertyFormat.Integer, Name = "Width", LongName = "Width" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(BuildDataTextureProperty.Height), Format = PropertyFormat.Integer, Name = "Height", LongName = "Height" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(BuildDataTextureProperty.Format), Format = PropertyFormat.String, Name = "Format", LongName = "Format" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(BuildDataTextureProperty.MipCount), Format = PropertyFormat.Integer, Name = "MipCount", LongName = "Number Of MipMap Levels" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(BuildDataTextureProperty.RwEnabled), Format = PropertyFormat.Bool, Name = "RwEnabled", LongName = "Read/Write Is Enabled" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(BuildDataTextureProperty.AssetBundle), Format = PropertyFormat.String, Name = "File", LongName = "File Name" },
            }
        };

        public override string Name => "BuildDataTextures";

        public override IReadOnlyCollection<IssueLayout> SupportedLayouts => new IssueLayout[]
        {
            k_TextureLayout
        };

        public override AnalysisResult Audit(AnalysisParams projectAuditorParams, IProgress progress = null)
        {
            var analyzers = GetPlatformAnalyzers(projectAuditorParams.Platform);

            if (projectAuditorParams.BuildObjects != null)
            {
                var textures = projectAuditorParams.BuildObjects.GetObjects<Texture2D>();

                progress?.Start("Parsing Texture2D from Build Data", "Search in Progress...", textures.Count());

                foreach (var texture in textures)
                {
                    foreach (var analyzer in analyzers)
                    {
                        var context = new BuildDataTexture2DAnalyzerContext
                        {
                            Texture = texture,
                            Params = projectAuditorParams
                        };

                        projectAuditorParams.OnIncomingIssues(analyzer.Analyze(context));
                    }

                    progress?.Advance();
                }

                progress?.Clear();
            }

            return AnalysisResult.Success;
        }
    }
}
