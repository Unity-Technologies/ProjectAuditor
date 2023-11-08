using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Interfaces;

namespace Unity.ProjectAuditor.Editor.Modules
{
    class SpriteModule : ProjectAuditorModuleWithAnalyzers<ISpriteAtlasModuleAnalyzer>
    {
        public override string name => "Sprites Atlas";

        public override bool isEnabledByDefault => false;

        public override IReadOnlyCollection<IssueLayout> supportedLayouts  => new IssueLayout[] { AssetsModule.k_IssueLayout };

        const string k_SpriteAtlasEmptySpaceLimit   = "SpriteAtlasEmptySpaceLimit";

        public override void RegisterParameters(ProjectAuditorDiagnosticParams diagnosticParams)
        {
            diagnosticParams.RegisterParameter(k_SpriteAtlasEmptySpaceLimit, 50);
        }

        public override void Audit(ProjectAuditorParams projectAuditorParams, IProgress progress = null)
        {
            var analyzers = GetPlatformAnalyzers(projectAuditorParams.Platform);
            var assetPaths = GetAssetPathsByFilter("t:SpriteAtlas, a:assets");

            progress?.Start("Finding Sprite Atlas", "Search in Progress...", assetPaths.Length);

            var spriteAtlasEmptySpaceLimit = projectAuditorParams.DiagnosticParams.GetParameter(k_SpriteAtlasEmptySpaceLimit);

            foreach (var assetPath in assetPaths)
            {
                var context = new SpriteAtlasAnalysisContext
                {
                    AssetPath = assetPath,
                    Params = projectAuditorParams,
                    SpriteAtlasEmptySpaceLimit = spriteAtlasEmptySpaceLimit
                };

                foreach (var analyzer in analyzers)
                {
                    projectAuditorParams.OnIncomingIssues(analyzer.Analyze(context));
                }

                progress?.Advance();
            }

            progress?.Clear();

            projectAuditorParams.OnModuleCompleted.Invoke();
        }
    }
}
