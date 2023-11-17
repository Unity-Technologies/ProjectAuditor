using System.Collections.Generic;
using System.Linq;
using Unity.ProjectAuditor.Editor.BuildData.SerializedObjects;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Interfaces;

namespace Unity.ProjectAuditor.Editor.Modules
{
    enum BuildDataTextureProperty
    {
        AssetBundle,
        Size,
        Width,
        Height,
        Format,
        MipCount,
        RwEnabled,
        Num,
    }

    class BuildDataTexture2DModule : ModuleWithAnalyzers<IBuildDataTexture2DModuleAnalyzer>
    {
        static readonly IssueLayout k_TextureLayout = new IssueLayout
        {
            category = IssueCategory.BuildDataTexture2D,
            properties = new[]
            {
                new PropertyDefinition { type = PropertyType.Description, format = PropertyFormat.String, name = "Name", longName = "Texture Name" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(BuildDataTextureProperty.AssetBundle), format = PropertyFormat.String, name = "File", longName = "File Name" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(BuildDataTextureProperty.Size), format = PropertyFormat.Bytes, name = "Size", longName = "Size" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(BuildDataTextureProperty.Width), format = PropertyFormat.Integer, name = "Width", longName = "Width" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(BuildDataTextureProperty.Height), format = PropertyFormat.Integer, name = "Height", longName = "Height" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(BuildDataTextureProperty.Format), format = PropertyFormat.String, name = "Format", longName = "Format" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(BuildDataTextureProperty.MipCount), format = PropertyFormat.Integer, name = "MipCount", longName = "Number Of MipMap Levels" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(BuildDataTextureProperty.RwEnabled), format = PropertyFormat.Bool, name = "RwEnabled", longName = "Read/Write Is Enabled" },
            }
        };

        public override string Name => "Textures";

        public override bool IsEnabledByDefault => false;

        public override IReadOnlyCollection<IssueLayout> SupportedLayouts => new IssueLayout[]
        {
            k_TextureLayout
        };

        public override void Audit(AnalysisParams projectAuditorParams, IProgress progress = null)
        {
            var analyzers = GetPlatformAnalyzers(projectAuditorParams.Platform);

            if (projectAuditorParams.BuildAnalyzer != null)
            {
                var textures = projectAuditorParams.BuildAnalyzer.GetSerializedObjects<Texture2D>();

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

            projectAuditorParams.OnModuleCompleted?.Invoke();
        }
    }
}
