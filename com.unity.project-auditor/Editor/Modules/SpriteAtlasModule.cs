using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;

namespace Unity.ProjectAuditor.Editor.Modules
{
    class SpriteModule : ModuleWithAnalyzers<SpriteAtlasModuleAnalyzer>
    {
        public override string Name => "Sprite Atlases";

        public override IReadOnlyCollection<IssueLayout> SupportedLayouts  => new IssueLayout[] { AssetsModule.k_IssueLayout };

        public override AnalysisResult Audit(AnalysisParams analysisParams, IProgress progress = null)
        {
            var analyzers = GetCompatibleAnalyzers(analysisParams);

            var context = new SpriteAtlasAnalysisContext
            {
                // AssetPath set in loop
                Params = analysisParams
            };

            var assetPaths = GetAssetPathsByFilter("t:SpriteAtlas, a:assets", context);

            progress?.Start("Finding Sprite Atlas", "Search in Progress...", assetPaths.Length);

            foreach (var assetPath in assetPaths)
            {
                if (progress?.IsCancelled ?? false)
                    return AnalysisResult.Cancelled;

                context.AssetPath = assetPath;

                foreach (var analyzer in analyzers)
                {
                    analysisParams.OnIncomingIssues(analyzer.Analyze(context));
                }

                progress?.Advance();
            }

            progress?.Clear();

            return AnalysisResult.Success;
        }
    }
}
