using System.Collections.Generic;
using System.Linq;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using Unity.ProjectAuditor.Editor.Modules;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace Unity.ProjectAuditor.Editor.SettingsAnalysis
{
    class Physics2DAnalyzer : ISettingsModuleAnalyzer
    {
        internal const string PAS0015 = nameof(PAS0015);
        internal const string PAS0032 = nameof(PAS0032);

        static readonly Descriptor k_DefaultLayerCollisionMatrixDescriptor = new Descriptor(
            PAS0015,
            "Physics2D: Layer Collision Matrix",
            new[] { Area.CPU },
            "In Physics 2D Settings, all of the boxes in the <b>Layer Collision Matrix</b> are ticked. This increases the CPU work that Unity must do when calculating collision detections.",
            "Un-tick all of the boxes except the ones that represent collisions that should be considered by the 2D physics system.");

        static readonly Descriptor k_SimulationModeDescriptor = new Descriptor(
            PAS0032,
            "Physics2D: Simulation Mode",
            new[] { Area.CPU },
            "<b>UnityEngine.Physics2D.simulationMode</b> is set to either <b>FixedUpdate</b> or <b>Update</b>. By using this mode, 2D physics simulation is executed on every update which might be expensive for some projects.",
            "Change <b>Project Settings ➔ Physics 2D ➔ General Settings ➔ Simulation Mode</b> to <b>Script</b> to disable the 2d physics processing each frame. If physics simulation is required for certain special rendering, use <b>Script</b> mode to control <b>Physics2d.Simulate</b> on a per frame basis.")
        {
            minimumVersion = "2020.2"
        };

        public void Initialize(ProjectAuditorModule module)
        {
            module.RegisterDescriptor(k_DefaultLayerCollisionMatrixDescriptor);
            module.RegisterDescriptor(k_SimulationModeDescriptor);
        }

        public IEnumerable<ProjectIssue> Analyze(ProjectAuditorParams projectAuditorParams)
        {
            if (IsDefaultLayerCollisionMatrix())
            {
                yield return ProjectIssue.Create(IssueCategory.ProjectSetting, k_DefaultLayerCollisionMatrixDescriptor)
                    .WithLocation("Project/Physics 2D");
            }
            if (IsNotUsingSimulationModeScript())
            {
                yield return ProjectIssue.Create(IssueCategory.ProjectSetting, k_SimulationModeDescriptor)
                    .WithLocation("Project/Physics 2D");
            }
        }

        internal static bool IsDefaultLayerCollisionMatrix()
        {
            const int numLayers = 32;
            for (var i = 0; i < numLayers; ++i)
                for (var j = 0; j < i; ++j)
                    if (Physics2D.GetIgnoreLayerCollision(i, j))
                        return false;
            return true;
        }

        static bool IsNotUsingSimulationModeScript()
        {
#if UNITY_2020_2_OR_NEWER
            return Physics2D.simulationMode != SimulationMode2D.Script;
#else
            return false;
#endif
        }
    }
}
