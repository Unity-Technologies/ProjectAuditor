using System.Collections.Generic;
using System.Linq;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using Unity.ProjectAuditor.Editor.Interfaces;
using Unity.ProjectAuditor.Editor.Modules;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace Unity.ProjectAuditor.Editor.SettingsAnalysis
{
    class PhysicsAnalyzer : ISettingsModuleAnalyzer
    {
        internal const string PAS0013 = nameof(PAS0013);

        static readonly Descriptor k_DefaultLayerCollisionMatrixDescriptor = new Descriptor(
            PAS0013,
            "Physics: Layer Collision Matrix has all boxes ticked",
            new[] { Area.CPU },
            "In Physics Settings, all of the boxes in the <b>Layer Collision Matrix</b> are ticked. This increases the CPU work required to calculate collision detections.",
            "Un-tick all of the boxes except the ones that represent collisions that should be considered by the Physics system."
        );

        public void Initialize(ProjectAuditorModule module)
        {
            module.RegisterDescriptor(k_DefaultLayerCollisionMatrixDescriptor);
        }

        public IEnumerable<ProjectIssue> Analyze(ProjectAuditorParams projectAuditorParams)
        {
            if (IsDefaultLayerCollisionMatrix())
            {
                yield return ProjectIssue.Create(IssueCategory.ProjectSetting, k_DefaultLayerCollisionMatrixDescriptor.id)
                    .WithLocation("Project/Physics");
            }
        }

        internal static bool IsDefaultLayerCollisionMatrix()
        {
            const int numLayers = 32;
            for (var i = 0; i < numLayers; ++i)
                for (var j = 0; j < i; ++j)
                    if (Physics.GetIgnoreLayerCollision(i, j))
                        return false;
            return true;
        }
    }
}
