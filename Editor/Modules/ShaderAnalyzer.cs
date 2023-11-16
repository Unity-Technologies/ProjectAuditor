using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using Unity.ProjectAuditor.Editor.Interfaces;
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
            "The shader is not compatible with SRP Batcher.",
            "Consider adding SRP Batcher compatibility to the shader. This will reduce the CPU time Unity requires to prepare and dispatch draw calls for materials that use the same shader variant."
        )
        {
            MessageFormat = "Shader '{0}' is not compatible with SRP Batcher",
            DocumentationUrl = "https://docs.unity3d.com/Manual/SRPBatcher.html"
        };

        public void Initialize(Module module)
        {
            module.RegisterDescriptor(k_SrpBatcherDescriptor);
        }

        public IEnumerable<ProjectIssue> Analyze(ShaderAnalysisContext context)
        {
            if (!IsSrpBatchingEnabled)
            {
                yield break;
            }

#if UNITY_2019_3_OR_NEWER
            var subShaderIndex = ShaderUtilProxy.GetShaderActiveSubshaderIndex(context.Shader);
            var isSrpBatchingCompatible = ShaderUtilProxy.GetSRPBatcherCompatibilityCode(context.Shader, subShaderIndex) == 0;

            if (!isSrpBatchingCompatible && IsSrpBatchingEnabled)
            {
                yield return context.Create(IssueCategory.AssetDiagnostic, k_SrpBatcherDescriptor.Id, context.Shader.name)
                    .WithLocation(context.AssetPath);
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
