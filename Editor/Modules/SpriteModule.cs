using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Interfaces;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine.U2D;

namespace Unity.ProjectAuditor.Editor.Modules
{
    class SpriteModule : ProjectAuditorModuleWithAnalyzers<ISpriteAtlasModuleAnalyzer>
    {
        public override string name => "Sprites Atlas";

        public override bool isEnabledByDefault => false;

        public override IReadOnlyCollection<IssueLayout> supportedLayouts  => new IssueLayout[] { AssetsModule.k_IssueLayout };

        public override void Audit(ProjectAuditorParams projectAuditorParams, IProgress progress = null)
        {
            var analyzers = GetPlatformAnalyzers(projectAuditorParams.Platform);

            var rules = projectAuditorParams.Rules;
            var spriteAtlasEmptySpaceLimit = rules.GetParameter("SpriteAtlasEmptySpaceLimit", 50);

            var context = new SpriteAtlasAnalysisContext
            {
                // AssetPath set in loop
                Params = projectAuditorParams,
                SpriteAtlasEmptySpaceLimit = spriteAtlasEmptySpaceLimit
            };

            var assetPaths = GetAssetPathsByFilter("t:SpriteAtlas, a:assets", context);

            progress?.Start("Finding Sprite Atlas", "Search in Progress...", assetPaths.Length);

            foreach (var assetPath in assetPaths)
            {
                context.AssetPath = assetPath;

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
