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
            var allSpriteAtlases = AssetDatabase.FindAssets("t:SpriteAtlas, a:assets");
            //var currentPlatformString = projectAuditorParams.platform.ToString();

            progress?.Start("Finding Sprite Atlas", "Search in Progress...", allSpriteAtlases.Length);

            foreach (var guid in allSpriteAtlases)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);

                foreach (var analyzer in analyzers)
                {
                    projectAuditorParams.OnIncomingIssues(analyzer.Analyze(projectAuditorParams, assetPath));
                }

                progress?.Advance();
            }

            progress?.Clear();

            projectAuditorParams.OnModuleCompleted.Invoke();
        }
    }
}
