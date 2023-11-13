using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Interfaces;

namespace Unity.ProjectAuditor.Editor.Modules
{
    class SpriteModule : ModuleWithAnalyzers<ISpriteAtlasModuleAnalyzer>
    {
        public override string Name => "Sprites Atlas";

        public override bool IsEnabledByDefault => false;

        public override IReadOnlyCollection<IssueLayout> SupportedLayouts  => new IssueLayout[] { AssetsModule.k_IssueLayout };

        const string k_SpriteAtlasEmptySpaceLimit   = "SpriteAtlasEmptySpaceLimit";

        public override void RegisterParameters(DiagnosticParams diagnosticParams)
        {
            diagnosticParams.RegisterParameter(k_SpriteAtlasEmptySpaceLimit, 50);
        }

        public override void Audit(AnalysisParams analysisParams, IProgress progress = null)
        {
            var analyzers = GetPlatformAnalyzers(analysisParams.Platform);

            var spriteAtlasEmptySpaceLimit = analysisParams.DiagnosticParams.GetParameter(k_SpriteAtlasEmptySpaceLimit);

            var context = new SpriteAtlasAnalysisContext
            {
                // AssetPath set in loop
                Params = analysisParams,
                SpriteAtlasEmptySpaceLimit = spriteAtlasEmptySpaceLimit
            };

            var assetPaths = GetAssetPathsByFilter("t:SpriteAtlas, a:assets", context);

            progress?.Start("Finding Sprite Atlas", "Search in Progress...", assetPaths.Length);

            foreach (var assetPath in assetPaths)
            {
                context.AssetPath = assetPath;

                foreach (var analyzer in analyzers)
                {
                    analysisParams.OnIncomingIssues(analyzer.Analyze(context));
                }

                progress?.Advance();
            }

            progress?.Clear();

            analysisParams.OnModuleCompleted.Invoke();
        }
    }
}
