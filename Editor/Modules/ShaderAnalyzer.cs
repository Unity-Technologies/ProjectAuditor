using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEngine;
using UnityEngine.Rendering;

namespace Unity.ProjectAuditor.Editor.Modules
{
    class ShaderAnalyzer : IShaderModuleAnalyzer
    {

        internal const string PAA2000 = nameof(PAA2000);

        internal static readonly Descriptor k_SrpBatcherDescriptor = new Descriptor(
            PAA2000,
            "Shader: Not compatible with SRP batcher",
            Area.CPU,
            "The shader is not compatible with SRP batcher.",
            "Consider adding SRP batcher compatibility to the shader. This will reduce the CPU time Unity requires to prepare and dispatch draw calls for materials that use the same shader variant."
        )
        {
            messageFormat = "Shader '{0}' is not compatible with SRP batcher",
            documentationUrl = "https://docs.unity3d.com/Manual/SRPBatcher.html"
        };

        public void Initialize(ProjectAuditorModule module)
        {
            module.RegisterDescriptor(k_SrpBatcherDescriptor);
        }

        public IEnumerable<ProjectIssue> Analyze(ProjectAuditorParams projectAuditorParams, Shader shader,
            string assetPath)
        {
            if (!IsSrpBatchingEnabled)
            {
                yield break;
            }

#if UNITY_2019_3_OR_NEWER
            var subShaderIndex = ShaderUtilProxy.GetShaderActiveSubshaderIndex(shader);
            var isSrpBatchingCompatible = ShaderUtilProxy.GetSRPBatcherCompatibilityCode(shader, subShaderIndex) == 0;

            if (!isSrpBatchingCompatible && IsSrpBatchingEnabled)
            {
                yield return ProjectIssue.Create(IssueCategory.AssetDiagnostic, k_SrpBatcherDescriptor, shader.name)
                    .WithLocation(assetPath);
            }
#endif
        }

#if UNITY_2019_3_OR_NEWER
        internal static bool IsSrpBatchingEnabled => GraphicsSettings.defaultRenderPipeline != null &&
                                                     GraphicsSettings.useScriptableRenderPipelineBatching;
#else
        internal static bool IsSrpBatchingEnabled => false;
#endif
    }
}
