using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine.U2D;

namespace Unity.ProjectAuditor.Editor.Modules
{
    class SpriteModule : ProjectAuditorModuleWithAnalyzers<ISpriteAtlasModuleAnalyzer>
    {
        internal static readonly IssueLayout k_IssueLayout = new IssueLayout
        {
            category = IssueCategory.AssetDiagnostic
        };

        internal override string name => "Sprites Atlas";

        internal override bool isEnabledByDefault => false;

        internal override IReadOnlyCollection<IssueLayout> supportedLayouts  => new IssueLayout[] { AssetsModule.k_IssueLayout };

        internal override void Audit(ProjectAuditorParams projectAuditorParams, IProgress progress = null)
        {
            var analyzers = GetPlatformAnalyzers(projectAuditorParams.platform);
            var allSpriteAtlases = AssetDatabase.FindAssets("t:SpriteAtlas, a:assets");
            //var currentPlatformString = projectAuditorParams.platform.ToString();

            progress?.Start("Finding Sprite Atlas", "Search in Progress...", allSpriteAtlases.Length);

            foreach (var guid in allSpriteAtlases)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);

                foreach (var analyzer in analyzers)
                {
                    projectAuditorParams.onIncomingIssues(analyzer.Analyze(projectAuditorParams, assetPath));
                }

                progress?.Advance();
            }

            progress?.Clear();

            projectAuditorParams.onModuleCompleted.Invoke();
        }
    }
}
